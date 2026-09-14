using Joana.Domain.Primitives;

namespace Joana.Domain.Calls;

public enum AttemptState
{
    Draft, PreflightReady, AwaitingConfirmation, Authorized, DialIntentPersisted,
    DialActionIssued, ObservingOutcome, Connected, DialogueActive, HandoffPending,
    HumanControlled, Ending, Ended, NotAnswered, OutcomeUnknown, Reconciling,
    ResolvedNoCall, ResolvedCallOccurred, Blocked,
    IntentPersisted = DialIntentPersisted,
    DialIssued = DialActionIssued,
    Uncertain = OutcomeUnknown,
    Failed = Blocked,
}

public sealed class CallAttempt
{
    public CallAttempt(AttemptId id) => Id = id;
    public AttemptId Id { get; }
    public AttemptState State { get; private set; } = AttemptState.Draft;
    public int Sequence { get; private set; }
    public int DialEmissionCount { get; private set; }
    public bool IsTerminal => State is AttemptState.NotAnswered or AttemptState.HumanControlled or AttemptState.Ended or AttemptState.ResolvedNoCall or AttemptState.ResolvedCallOccurred or AttemptState.Blocked;

    public OperationResult MoveTo(AttemptState next) => Sequence == int.MaxValue ? OperationResult.Fail("InvalidSequence") : MoveTo(next, Sequence + 1);

    public OperationResult MoveTo(AttemptState next, int expectedSequence)
    {
        if (expectedSequence <= Sequence) return OperationResult.Fail("StaleOrDuplicateEvent");
        if (IsTerminal || !IsAllowedTransition(State, next)) return OperationResult.Fail("InvalidStateTransition");
        if (next == AttemptState.DialActionIssued && DialEmissionCount != 0) return OperationResult.Fail("DialAlreadyIssued");
        if (next == AttemptState.DialActionIssued) DialEmissionCount = 1;
        State = next;
        Sequence = expectedSequence;
        return OperationResult.Ok();
    }

    private static bool IsAllowedTransition(AttemptState current, AttemptState next) => (current, next) switch
    {
        (AttemptState.Draft, AttemptState.PreflightReady) or (AttemptState.Draft, AttemptState.Blocked) => true,
        (AttemptState.PreflightReady, AttemptState.AwaitingConfirmation) or (AttemptState.PreflightReady, AttemptState.Blocked) => true,
        (AttemptState.AwaitingConfirmation, AttemptState.Authorized) or (AttemptState.AwaitingConfirmation, AttemptState.Blocked) => true,
        (AttemptState.Authorized, AttemptState.DialIntentPersisted) or (AttemptState.Authorized, AttemptState.Blocked) => true,
        (AttemptState.DialIntentPersisted, AttemptState.DialActionIssued) or (AttemptState.DialIntentPersisted, AttemptState.Blocked) => true,
        (AttemptState.DialActionIssued, AttemptState.ObservingOutcome) => true,
        (AttemptState.ObservingOutcome, AttemptState.Connected) or (AttemptState.ObservingOutcome, AttemptState.NotAnswered) or (AttemptState.ObservingOutcome, AttemptState.OutcomeUnknown) => true,
        (AttemptState.Connected, AttemptState.DialogueActive) => true,
        (AttemptState.DialogueActive, AttemptState.HandoffPending) or (AttemptState.DialogueActive, AttemptState.Ending) => true,
        (AttemptState.HandoffPending, AttemptState.HumanControlled) or (AttemptState.HandoffPending, AttemptState.Ending) => true,
        (AttemptState.Ending, AttemptState.Ended) => true,
        (AttemptState.OutcomeUnknown, AttemptState.Reconciling) => true,
        (AttemptState.Reconciling, AttemptState.ResolvedNoCall) or (AttemptState.Reconciling, AttemptState.ResolvedCallOccurred) or (AttemptState.Reconciling, AttemptState.Blocked) => true,
        _ => false,
    };
}