namespace Joana.Desktop.Composition;
public static class StartupDiagnostics { public static IReadOnlyList<string> Evaluate(string mode) => mode.Equals("Simulation", StringComparison.OrdinalIgnoreCase) ? ["Mode:Simulation", "Timezone:America/Sao_Paulo", "ProductionAdapters:None"] : ["ProductionUnavailable", "VoiceVendorUnapproved", "PhoneLinkRealDisabled"]; }
