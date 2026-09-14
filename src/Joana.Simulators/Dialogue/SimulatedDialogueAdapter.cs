using Joana.Application.Ports;
namespace Joana.Simulators.Dialogue;
public sealed class SimulatedDialogueAdapter : IDialogueAdapter { public Task<IReadOnlyList<string>> StartAsync(IReadOnlyList<string> allowedFacts, IReadOnlyList<string> questions, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([.. allowedFacts, .. questions]); }
