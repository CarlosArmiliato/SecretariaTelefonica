namespace Joana.Application.Ports;
public interface ITimeSource { DateTimeOffset UtcNow { get; } long MonotonicTimestamp { get; } long MonotonicFrequency { get; } }
