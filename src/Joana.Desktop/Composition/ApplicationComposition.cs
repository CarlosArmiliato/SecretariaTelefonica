using Joana.Application.Orchestration;
using Joana.Application.Ports;
using Joana.Infrastructure.Persistence;
using Joana.Simulators.PhoneLink;

namespace Joana.Desktop.Composition;

public static class ApplicationComposition
{
    public const string SimulationReady = "SimulationReady";
    public const string ProductionUnavailable = "ProductionUnavailable";

    public static string Create(string mode) =>
        string.Equals(mode, "Simulation", StringComparison.OrdinalIgnoreCase)
            ? SimulationReady
            : ProductionUnavailable;

    public static SimulationServices? CreateSimulation(string mode, string stateRoot)
    {
        if (!string.Equals(mode, "Simulation", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new SimulationServices(stateRoot);
    }
}

public sealed class SimulationServices
{
    public SimulationServices(string stateRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateRoot);
        Preflight = new PreflightCoordinator(new SystemTimeSource());
        StateRoot = Path.GetFullPath(stateRoot);
    }

    public string StateRoot { get; }

    public PreflightCoordinator Preflight { get; }

    public CallAttemptCoordinator CreateCallCoordinator(string destination) =>
        new(new SimulatedPhoneLinkAdapter(destination), new FileAttemptStore(StateRoot));

    private sealed class SystemTimeSource : ITimeSource
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public long MonotonicTimestamp => System.Diagnostics.Stopwatch.GetTimestamp();
        public long MonotonicFrequency => System.Diagnostics.Stopwatch.Frequency;
    }
}
