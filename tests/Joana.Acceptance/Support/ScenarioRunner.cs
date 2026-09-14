namespace Joana.Acceptance.Support;
public sealed class ScenarioRunner { public static bool IsSimulation(string mode) => string.Equals(mode, "Simulation", StringComparison.OrdinalIgnoreCase); }
