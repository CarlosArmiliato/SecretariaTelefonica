namespace Joana.Application.Ports;
/// <summary>Injectable UTC civil time plus monotonic ticks; implementations must not use local time for deadlines.</summary>
public interface ITimeSource { DateTimeOffset UtcNow { get; } long MonotonicTimestamp { get; } long MonotonicFrequency { get; } }