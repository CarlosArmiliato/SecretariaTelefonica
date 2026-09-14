namespace Joana.Acceptance.Cases.Authorization;

/// <summary>
/// T014 acceptance contract for the operator commands used by the preparation panel.
/// The harness is a simulation-only seam. These assertions are intentionally written before
/// the command implementation so the suite fails until every guard is enforced by the controller.
/// </summary>
public static class OperatorCommandCases
{
    public static async Task<IReadOnlyList<string>> RunAsync(
        IOperatorCommandHarness harness,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(harness);

        var failures = new List<string>();

        await CheckReviewPreflightAsync(harness, failures, cancellationToken);
        await CheckRequestDialConfirmationAsync(harness, failures, cancellationToken);
        await CheckConfirmAndDialAsync(harness, failures, cancellationToken);
        await CheckCancelDialAsync(harness, failures, cancellationToken);

        foreach (var reason in Enum.GetValues<OperatorBlockReason>())
        {
            await CheckBlockedConfirmAndDialAsync(harness, reason, failures, cancellationToken);
        }

        return failures;
    }

    private static async Task CheckReviewPreflightAsync(
        IOperatorCommandHarness harness,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await harness.ReviewPreflightAsync(cancellationToken);

        if (!outcome.Succeeded ||
            !outcome.ImmutableRevisionCreated ||
            outcome.ConfirmationOpen ||
            outcome.ConfirmationConsumed ||
            outcome.ConfirmationInvalidated ||
            outcome.SimulatedDialEmissions != 0 ||
            outcome.BlockedBy is not null)
        {
            failures.Add("T014.review-preflight-creates-immutable-revision");
        }
    }

    private static async Task CheckRequestDialConfirmationAsync(
        IOperatorCommandHarness harness,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await harness.RequestDialConfirmationAsync(cancellationToken);

        if (!outcome.Succeeded ||
            !outcome.ConfirmationOpen ||
            !outcome.ShowsCompanyNumberAndObjective ||
            outcome.ConfirmationConsumed ||
            outcome.ConfirmationInvalidated ||
            outcome.SimulatedDialEmissions != 0 ||
            outcome.BlockedBy is not null)
        {
            failures.Add("T014.request-dial-confirmation-shows-binding");
        }
    }

    private static async Task CheckConfirmAndDialAsync(
        IOperatorCommandHarness harness,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await harness.ConfirmAndDialAsync(cancellationToken);

        if (!outcome.Succeeded ||
            outcome.SimulatedDialEmissions != 1 ||
            !outcome.ConfirmationConsumed ||
            outcome.ConfirmationOpen ||
            outcome.ConfirmationInvalidated ||
            outcome.BlockedBy is not null)
        {
            failures.Add("T014.confirm-and-dial-consumes-confirmation-once");
        }
    }

    private static async Task CheckCancelDialAsync(
        IOperatorCommandHarness harness,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await harness.CancelDialAsync(cancellationToken);

        if (!outcome.Succeeded ||
            !outcome.ConfirmationInvalidated ||
            outcome.ConfirmationOpen ||
            outcome.ConfirmationConsumed ||
            outcome.SimulatedDialEmissions != 0 ||
            outcome.BlockedBy is not null)
        {
            failures.Add("T014.cancel-dial-invalidates-without-effect");
        }
    }

    private static async Task CheckBlockedConfirmAndDialAsync(
        IOperatorCommandHarness harness,
        OperatorBlockReason reason,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await harness.AttemptBlockedConfirmAndDialAsync(reason, cancellationToken);

        var mustInvalidateConfirmation = reason is
            OperatorBlockReason.PreflightChanged or
            OperatorBlockReason.UncertainState or
            OperatorBlockReason.StorageFailure or
            OperatorBlockReason.ConsumedConfirmation;
        if (outcome.Succeeded ||
            outcome.BlockedBy != reason ||
            outcome.ConfirmationConsumed ||
            outcome.SimulatedDialEmissions != 0 ||
            (mustInvalidateConfirmation && (!outcome.ConfirmationInvalidated || outcome.ConfirmationOpen)))
        {
            failures.Add($"T014.confirm-and-dial-blocked-{reason}");
        }
    }
}

public enum OperatorBlockReason
{
    NotSimulation,
    SupervisionAbsent,
    PreflightChanged,
    UncertainState,
    SecondInstance,
    StorageFailure,
    ConsumedConfirmation,
}

public sealed record OperatorCommandOutcome(
    bool Succeeded,
    bool ImmutableRevisionCreated,
    bool ConfirmationOpen,
    bool ShowsCompanyNumberAndObjective,
    bool ConfirmationConsumed,
    bool ConfirmationInvalidated,
    int SimulatedDialEmissions)
{
    /// <summary>Stable scenario evidence; never contains request data or exception text.</summary>
    public OperatorBlockReason? BlockedBy { get; init; }
}

/// <summary>Simulation-only seam; production UI must not provide a real Phone Link implementation.</summary>
public interface IOperatorCommandHarness
{
    // Each command method is evaluated against an isolated synthetic setup. No real UI,
    // Phone Link, audio adapter, network access, or environment data is involved.
    Task<OperatorCommandOutcome> ReviewPreflightAsync(CancellationToken cancellationToken);
    Task<OperatorCommandOutcome> RequestDialConfirmationAsync(CancellationToken cancellationToken);
    Task<OperatorCommandOutcome> ConfirmAndDialAsync(CancellationToken cancellationToken);
    Task<OperatorCommandOutcome> CancelDialAsync(CancellationToken cancellationToken);
    Task<OperatorCommandOutcome> AttemptBlockedConfirmAndDialAsync(OperatorBlockReason reason, CancellationToken cancellationToken);
}
