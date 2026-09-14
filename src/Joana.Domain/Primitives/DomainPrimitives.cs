namespace Joana.Domain.Primitives;

/// <summary>Opaque identifier that is safe to correlate without carrying call content.</summary>
public readonly record struct AttemptId(Guid Value) { public static AttemptId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString("N"); }
/// <summary>Opaque identifier for an immutable preflight revision.</summary>
public readonly record struct PreflightRevisionId(Guid Value) { public static PreflightRevisionId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString("N"); }
/// <summary>Opaque identifier for a human-takeover request.</summary>
public readonly record struct HandoffId(Guid Value) { public static HandoffId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString("N"); }
/// <summary>Structured outcome containing a stable code only; never persist exception or user content here.</summary>
public sealed record OperationResult(bool Succeeded, string Code) { public static OperationResult Ok(string code = "Ok") => new(true, code); public static OperationResult Fail(string code) => new(false, code); }
/// <summary>Injectable monotonic clock used only for durations and deadlines.</summary>
public interface IMonotonicClock { long Timestamp { get; } long Frequency { get; } }