using Joana.Domain.Primitives;

namespace Joana.Domain.Handoff;

public enum HandoffOutcome { Pending, TakenOver, TimedOut, Failed, SafelyEnded }
public sealed class HumanHandoff
{
    public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    public HumanHandoff(string reason, DateTimeOffset startedAtUtc, long startedTimestamp) { Id = HandoffId.New(); Reason = reason; StartedAtUtc = startedAtUtc; StartedTimestamp = startedTimestamp; }
    public HandoffId Id { get; }
    public string Reason { get; }
    public DateTimeOffset StartedAtUtc { get; }
    public long StartedTimestamp { get; }
    public bool VoiceStopped { get; private set; }
    public HandoffOutcome Outcome { get; private set; } = HandoffOutcome.Pending;
    public void MarkVoiceStopped() => VoiceStopped = true;
    public bool Confirm(HandoffId id, long now, long frequency) { if (id != Id || Outcome != HandoffOutcome.Pending || now - StartedTimestamp >= Deadline.TotalSeconds * frequency) return false; Outcome = HandoffOutcome.TakenOver; return true; }
    public bool Expire(long now, long frequency) { if (Outcome != HandoffOutcome.Pending || now - StartedTimestamp < Deadline.TotalSeconds * frequency) return false; Outcome = HandoffOutcome.TimedOut; return true; }
    public void Fail() { if (Outcome == HandoffOutcome.Pending) Outcome = HandoffOutcome.Failed; }
    public void MarkSafelyEnded() => Outcome = HandoffOutcome.SafelyEnded;
}
