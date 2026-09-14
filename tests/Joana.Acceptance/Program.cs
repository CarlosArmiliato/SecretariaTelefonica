using System.Text.Json;
using Joana.Acceptance.Support;

var result = await new ScenarioRunner().RunAsync(args, CancellationToken.None);
Console.WriteLine(JsonSerializer.Serialize(result, ScenarioRunner.JsonOptions));
return result.Passed ? 0 : 1;
