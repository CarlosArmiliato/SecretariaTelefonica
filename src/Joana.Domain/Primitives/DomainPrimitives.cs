namespace Joana.Domain.Primitives;

public readonly record struct AttemptId(Guid Value)
{
    public static AttemptId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct PreflightRevisionId(Guid Value)
{
    public static PreflightRevisionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct HandoffId(Guid Value)
{
    public static HandoffId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public sealed record OperationResult(bool Succeeded, string Code)
{
    public static OperationResult Ok(string code = "Ok") => new(true, code);
    public static OperationResult Fail(string code) => new(false, code);
}

public interface IMonotonicClock { long Timestamp { get; } long Frequency { get; } }
