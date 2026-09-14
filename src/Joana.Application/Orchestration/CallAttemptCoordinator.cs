using Joana.Application.Ports;
using Joana.Domain.Calls;
using Joana.Domain.Primitives;

namespace Joana.Application.Orchestration;

/// <summary>
/// Coordinates the single simulated dial side effect. Persistence and the
/// attempt state machine are the idempotency boundary; the Phone Link adapter
/// is never called again after an intent or an uncertain outcome exists.
/// </summary>
public sealed class CallAttemptCoordinator(IPhoneLinkAdapter phone, IAttemptStore attempts)
{
    private const string PhoneLinkApplication = "PhoneLink";

    public async Task<OperationResult> ConfirmAndDialAsync(
        CallAttempt attempt,
        PreflightRevision revision,
        DialAuthorization authorization,
        bool simulationMode,
        bool supervisionPresent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(revision);
        ArgumentNullException.ThrowIfNull(authorization);

        // This coordinator has no production path. These guards run before any
        // adapter or persistence operation that could have an external effect.
        if (!simulationMode) return OperationResult.Fail("SimulationRequired");
        if (!supervisionPresent) return OperationResult.Fail("SupervisionRequired");
        if (attempt.State != AttemptState.Authorized) return OperationResult.Fail("StateBlocked");

        bool ambiguousState;
        try
        {
            ambiguousState = await attempts.HasAmbiguousStateAsync(attempt.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            authorization.Invalidate("AttemptStateUnavailable");
            return OperationResult.Fail("AttemptStateUnavailable");
        }

        if (ambiguousState)
        {
            authorization.Invalidate("AmbiguousState");
            return OperationResult.Fail("AmbiguousState");
        }

        PhoneObservation observed;
        try
        {
            // Observation is intentionally consumed by the adapter only when
            // DialOnceAsync is called. No stale observation is reused later.
            observed = await phone.ObserveAsync(attempt.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            authorization.Invalidate("ObservationFailed");
            return OperationResult.Fail("ObservationFailed");
        }

        if (!IsValidDialObservation(observed, revision, authorization))
        {
            // A changed destination, focus or call state invalidates this
            // confirmation. A new attempt must receive a new confirmation.
            authorization.Invalidate("ObservationInvalid");
            return OperationResult.Fail("ObservationInvalid");
        }

        // Consume before changing durable state or invoking the adapter. A
        // concurrent click therefore cannot reuse this authorization.
        if (!authorization.Consume(revision, supervisionPresent).Succeeded)
        {
            return OperationResult.Fail("AuthorizationInvalid");
        }

        if (!attempt.MoveTo(AttemptState.DialIntentPersisted).Succeeded)
        {
            BlockAttempt(attempt);
            return OperationResult.Fail("StateBlocked");
        }

        OperationResult persisted;
        try
        {
            persisted = await attempts.CreateIntentAsync(attempt, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A cancellation during persistence cannot be used as evidence
            // that an intent was absent. Keep the attempt non-retryable.
            BlockAttempt(attempt);
            throw;
        }
        catch (Exception)
        {
            BlockAttempt(attempt);
            return OperationResult.Fail("IntentPersistenceFailed");
        }

        if (!persisted.Succeeded)
        {
            BlockAttempt(attempt);
            return persisted;
        }

        DialResult dialResult;
        try
        {
            // Use the overload that proves the adapter received the persisted
            // intent, never the pre-persistence convenience overload.
            dialResult = await phone.DialOnceAsync(attempt.Id, attempt, observed, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MarkUncertain(attempt);
            throw;
        }
        catch (Exception)
        {
            // An exception may have happened after the UI emitted the action.
            // Treat it as unknown and reconcile; never call DialOnceAsync again.
            dialResult = DialResult.Unknown;
        }

        if (dialResult == DialResult.Rejected)
        {
            return await HandleRejectedAsync(attempt, revision, authorization, cancellationToken);
        }

        // Issued and Unknown both require a state transition that records the
        // single possible emission before a fresh observation is requested.
        MarkDialActionIssued(attempt);
        if (attempt.State != AttemptState.DialActionIssued)
        {
            return OperationResult.Fail("StateBlocked");
        }

        if (!attempt.MoveTo(AttemptState.ObservingOutcome).Succeeded)
        {
            MarkUncertain(attempt);
            return OperationResult.Fail("StateBlocked");
        }

        PhoneObservation? reobserved;
        try
        {
            // The first observation token was consumed by the dial action.
            // This is a mandatory, fresh observation and is never reused.
            reobserved = await phone.ObserveAsync(attempt.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MarkUncertain(attempt);
            throw;
        }
        catch (Exception)
        {
            reobserved = null;
        }

        if (dialResult == DialResult.Unknown ||
            reobserved is null ||
            !IsValidOutcomeObservation(reobserved, revision, authorization))
        {
            MarkUncertain(attempt);
            return OperationResult.Fail("DialOutcomeUnknown");
        }

        if (reobserved.CallActive)
        {
            return attempt.MoveTo(AttemptState.Connected).Succeeded
                ? OperationResult.Ok("Connected")
                : OperationResult.Fail("StateBlocked");
        }

        return attempt.MoveTo(AttemptState.NotAnswered).Succeeded
            ? OperationResult.Ok("NotAnswered")
            : OperationResult.Fail("StateBlocked");
    }

    private async Task<OperationResult> HandleRejectedAsync(
        CallAttempt attempt,
        PreflightRevision revision,
        DialAuthorization authorization,
        CancellationToken cancellationToken)
    {
        PhoneObservation? reobserved;
        try
        {
            // Rejected still consumes the adapter observation token. Observe
            // again before deciding whether the rejection is trustworthy.
            reobserved = await phone.ObserveAsync(attempt.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MarkUncertain(attempt);
            throw;
        }
        catch (Exception)
        {
            reobserved = null;
        }

        if (reobserved is not null &&
            IsValidOutcomeObservation(reobserved, revision, authorization) &&
            !reobserved.CallActive)
        {
            BlockAttempt(attempt);
            return OperationResult.Fail("DialRejected");
        }

        // A contradictory or unavailable post-action observation is itself
        // uncertain. It blocks retry and records a conservative possible dial.
        MarkUncertain(attempt);
        return OperationResult.Fail("DialOutcomeUnknown");
    }

    private static bool IsValidDialObservation(
        PhoneObservation observation,
        PreflightRevision revision,
        DialAuthorization authorization) =>
        observation is not null &&
        !string.IsNullOrWhiteSpace(observation.Token) &&
        observation.Application == PhoneLinkApplication &&
        observation.Focused &&
        !observation.CallActive &&
        observation.VisibleDestination == revision.Request.PhoneNumber &&
        observation.VisibleDestination == authorization.PhoneNumber;

    private static bool IsValidOutcomeObservation(
        PhoneObservation observation,
        PreflightRevision revision,
        DialAuthorization authorization) =>
        observation is not null &&
        !string.IsNullOrWhiteSpace(observation.Token) &&
        observation.Application == PhoneLinkApplication &&
        observation.Focused &&
        observation.VisibleDestination == revision.Request.PhoneNumber &&
        observation.VisibleDestination == authorization.PhoneNumber;

    private static void BlockAttempt(CallAttempt attempt)
    {
        _ = attempt.MoveTo(AttemptState.Blocked);
    }

    private static void MarkDialActionIssued(CallAttempt attempt)
    {
        if (attempt.State == AttemptState.DialIntentPersisted)
        {
            _ = attempt.MoveTo(AttemptState.DialActionIssued);
        }
    }

    private static void MarkUncertain(CallAttempt attempt)
    {
        if (attempt.State == AttemptState.DialIntentPersisted)
        {
            _ = attempt.MoveTo(AttemptState.DialActionIssued);
        }

        if (attempt.State == AttemptState.DialActionIssued)
        {
            _ = attempt.MoveTo(AttemptState.ObservingOutcome);
        }

        if (attempt.State == AttemptState.ObservingOutcome)
        {
            _ = attempt.MoveTo(AttemptState.OutcomeUnknown);
        }

        if (attempt.State == AttemptState.OutcomeUnknown)
        {
            _ = attempt.MoveTo(AttemptState.Reconciling);
        }
    }
}
