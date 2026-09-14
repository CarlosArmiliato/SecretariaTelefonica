namespace Joana.Desktop.Composition;
public static class ApplicationComposition { public static string Create(string mode) => mode.Equals("Simulation", StringComparison.OrdinalIgnoreCase) ? "SimulationReady" : "ProductionUnavailable"; }
