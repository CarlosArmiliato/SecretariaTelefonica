using Joana.Application.Ports;
using Joana.Domain.Guardrails;

namespace Joana.Application.Orchestration;
public sealed class DialogueCoordinator(IDialogueAdapter dialogue, DialoguePolicy policy)
{
    public const string Introduction = "Sou Joana, a secretaria virtual com inteligencia artificial do Carlos.";
    public async Task<(GuardrailDecision Decision, IReadOnlyList<string> Lines)> StartAsync(string intent, IReadOnlyList<string> facts, IReadOnlyList<string> questions, CancellationToken cancellationToken)
    { var decision = policy.Evaluate(intent); if (decision.Action != GuardrailAction.Allow) return (decision, []); var lines = await dialogue.StartAsync(facts, questions, cancellationToken); return (decision, [Introduction, .. lines]); }
}
