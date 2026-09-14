using Joana.Domain.Primitives;

namespace Joana.Domain.Calls;

public enum AttemptState { Draft, Reviewed, AwaitingConfirmation, IntentPersisted, DialIssued, Connected, NotAnswered, Uncertain, Transferred, Ended, Failed }

public sealed class CallAttempt
{
    public CallAttempt(AttemptId id) => Id = id;
    public AttemptId Id { get; }
    public AttemptState State { get; private set; } = AttemptState.Draft;
    public int Sequence { get; private set; }
    public int DialEmissionCount { get; private set; }
    public bool IsTerminal => State is AttemptState.NotAnswered or AttemptState.Uncertain or AttemptState.Transferred or AttemptState.Ended or AttemptState.Failed;
    public OperationResult MoveTo(AttemptState next)
    {
        if (IsTerminal || next == State || (next == AttemptState.DialIssued && DialEmissionCount == 1)) return OperationResult.Fail("InvalidStateTransition");
        if (next == AttemptState.DialIssued) DialEmissionCount++;
        State = next; Sequence++;
        return OperationResult.Ok();
    }
}
