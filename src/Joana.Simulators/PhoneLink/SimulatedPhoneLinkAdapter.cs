using Joana.Application.Ports;
using Joana.Domain.Calls;
using Joana.Domain.Primitives;

namespace Joana.Simulators.PhoneLink;

/// <summary>
/// Deterministic Phone Link double. It has no dependency on Phone Link, Windows UI,
/// audio devices, or any external process.
/// </summary>
public sealed class SimulatedPhoneLinkAdapter : IPhoneLinkAdapter
{
    private const string ApplicationName = "PhoneLink";
    private const string WindowName = "SimulatedPhoneLink";
    private readonly object sync = new();
    private string? currentObservationToken;
    private AttemptId? currentObservationAttemptId;
    private bool callActive;
    private bool endPending;
    private PhoneCallPhase phase = PhoneCallPhase.Idle;

    public SimulatedPhoneLinkAdapter(string destination)
    {
        VisibleDestination = destination;
    }

    /// <summary>Number of simulated dial effects emitted by this adapter.</summary>
    public int EmissionCount { get; private set; }

    /// <summary>Alias for the dial-effect counter used by the call state model.</summary>
    public int DialEmissionCount => EmissionCount;

    /// <summary>Number of observations emitted, useful for asserting observe-act-observe flows.</summary>
    public int ObservationEmissionCount { get; private set; }

    public int EndEmissionCount { get; private set; }

    /// <summary>Injects a missing Phone Link focus into the next observation.</summary>
    public bool Focused { get; set; } = true;

    /// <summary>Injects the destination currently visible in the simulated UI.</summary>
    public string VisibleDestination { get; set; }

    /// <summary>Alias retained for concise scenario setup.</summary>
    public string Destination
    {
        get => VisibleDestination;
        set => VisibleDestination = value;
    }

    /// <summary>Causes a dial to be issued but subsequently observed as not answered.</summary>
    public bool SimulateNotAnswered { get; set; }

    public bool NotAnswered
    {
        get => SimulateNotAnswered;
        set => SimulateNotAnswered = value;
    }

    /// <summary>Causes an operation return to be indeterminate after its simulated effect.</summary>
    public bool SimulateTimeout { get; set; }

    public bool Timeout
    {
        get => SimulateTimeout;
        set => SimulateTimeout = value;
    }

    /// <summary>Causes a dial return to be lost after the simulated effect.</summary>
    public bool SimulateLostReturn { get; set; }

    public bool LostReturn
    {
        get => SimulateLostReturn;
        set => SimulateLostReturn = value;
    }

    /// <summary>Compatibility switch for existing lost-return scenarios.</summary>
    public bool ReturnUnknown { get; set; }

    /// <summary>Causes an end command to fail before a completed end is observable.</summary>
    public bool SimulateEndFailure { get; set; }

    public bool FailEnd
    {
        get => SimulateEndFailure;
        set => SimulateEndFailure = value;
    }

    /// <summary>Causes an end command to time out without claiming that the call ended.</summary>
    public bool SimulateEndTimeout { get; set; }

    public bool EndTimeout
    {
        get => SimulateEndTimeout;
        set => SimulateEndTimeout = value;
    }

    public Task<PhoneObservation> ObserveAsync(AttemptId attemptId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (sync)
        {
            // A successful end is only confirmed by this fresh observation, never EndAsync's return.
            if (endPending)
            {
                endPending = false;
                callActive = false;
                phase = PhoneCallPhase.Idle;
            }

            currentObservationToken = Guid.NewGuid().ToString("N");
            currentObservationAttemptId = attemptId;
            ObservationEmissionCount++;
            return Task.FromResult(new PhoneObservation(ApplicationName, Focused, VisibleDestination, callActive, currentObservationToken)
            {
                Window = WindowName,
                Phase = phase
            });
        }
    }

    public Task<DialResult> DialOnceAsync(AttemptId attemptId, PhoneObservation observation, CancellationToken cancellationToken)
        => DialOnceCoreAsync(attemptId, observation, cancellationToken);

    public Task<DialResult> DialOnceAsync(
        AttemptId attemptId,
        CallAttempt persistedIntent,
        PhoneObservation observation,
        CancellationToken cancellationToken)
    {
        if (persistedIntent is null)
        {
            return Task.FromResult(DialResult.Rejected);
        }

        return DialOnceCoreAsync(attemptId, observation, cancellationToken);
    }

    private Task<DialResult> DialOnceCoreAsync(AttemptId attemptId, PhoneObservation observation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (sync)
        {
            if (observation is null ||
                !TryConsumeCurrentObservation(attemptId, observation) ||
                observation.Application != ApplicationName ||
                !Focused ||
                !observation.Focused ||
                observation.CallActive ||
                observation.VisibleDestination != VisibleDestination)
            {
                return Task.FromResult(DialResult.Rejected);
            }

            EmissionCount++;

            if (SimulateNotAnswered)
            {
                callActive = false;
                phase = PhoneCallPhase.Idle;
                return Task.FromResult(DialResult.Issued);
            }

            if (SimulateTimeout || SimulateLostReturn || ReturnUnknown)
            {
                // The effect occurred, but its return is not trustworthy. A fresh
                // observation therefore shows the call as active and lets the
                // coordinator reconcile without issuing another dial.
                callActive = true;
                phase = PhoneCallPhase.Connected;
                return Task.FromResult(DialResult.Unknown);
            }

            callActive = true;
            phase = PhoneCallPhase.Connected;
            return Task.FromResult(DialResult.Issued);
        }
    }

    public Task<OperationResult> EndAsync(AttemptId attemptId, PhoneObservation observation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (sync)
        {
            if (observation is null ||
                !TryConsumeCurrentObservation(attemptId, observation) ||
                observation.Application != ApplicationName ||
                !Focused ||
                !observation.Focused ||
                observation.VisibleDestination != VisibleDestination ||
                !observation.CallActive ||
                !callActive)
            {
                return Task.FromResult(OperationResult.Fail("EndObservationInvalid"));
            }

            EndEmissionCount++;
            if (SimulateEndFailure)
            {
                return Task.FromResult(OperationResult.Fail("SyntheticEndFailure"));
            }

            if (SimulateEndTimeout || SimulateTimeout)
            {
                // As with a lost dial return, the end effect may already have
                // happened. Only the next observation is allowed to confirm it.
                phase = PhoneCallPhase.Ending;
                endPending = true;
                return Task.FromResult(OperationResult.Fail("SyntheticEndTimeout"));
            }

            // Keep the call active until a new observation proves it has ended.
            phase = PhoneCallPhase.Ending;
            endPending = true;
            return Task.FromResult(OperationResult.Ok("EndIssued"));
        }
    }

    private bool TryConsumeCurrentObservation(AttemptId attemptId, PhoneObservation observation)
    {
        if (currentObservationToken is null ||
            !currentObservationAttemptId.HasValue ||
            currentObservationAttemptId.Value != attemptId ||
            currentObservationToken != observation.Token)
        {
            return false;
        }

        currentObservationToken = null;
        currentObservationAttemptId = null;
        return true;
    }
}
