using Joana.Desktop.Composition;
var mode = args.SkipWhile(x => x != "--mode").Skip(1).FirstOrDefault() ?? "Simulation";
foreach (var diagnostic in StartupDiagnostics.Evaluate(mode)) Console.WriteLine(diagnostic);
Environment.ExitCode = ApplicationComposition.Create(mode) == "SimulationReady" ? 0 : 2;
