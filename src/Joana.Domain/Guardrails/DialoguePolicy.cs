namespace Joana.Domain.Guardrails;

public sealed class DialoguePolicy
{
    private static readonly Dictionary<string, ProhibitedCategory> Forbidden = new(StringComparer.OrdinalIgnoreCase)
    { ["emergencia"] = ProhibitedCategory.Emergency, ["medico"] = ProhibitedCategory.Medical, ["pagamento"] = ProhibitedCategory.Payment, ["cartao"] = ProhibitedCategory.Payment, ["senha"] = ProhibitedCategory.Authentication, ["token"] = ProhibitedCategory.Authentication, ["cpf"] = ProhibitedCategory.Document, ["juridico"] = ProhibitedCategory.Legal, ["multa"] = ProhibitedCategory.Penalty, ["instalar"] = ProhibitedCategory.Security, ["aceitar"] = ProhibitedCategory.Commitment, ["agendar"] = ProhibitedCategory.Commitment };
    public GuardrailDecision Evaluate(string intent)
    {
        if (string.IsNullOrWhiteSpace(intent)) return GuardrailDecision.Unknown();
        foreach (var item in Forbidden) if (intent.Contains(item.Key, StringComparison.OrdinalIgnoreCase)) return new(GuardrailAction.Handoff, item.Value, "ProhibitedCategory");
        return intent.Equals("allowed", StringComparison.OrdinalIgnoreCase) ? new(GuardrailAction.Allow, ProhibitedCategory.Unknown, "Allowed") : GuardrailDecision.Unknown();
    }
}
