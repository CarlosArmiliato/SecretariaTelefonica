namespace Joana.Domain.Guardrails;

public enum GuardrailAction { Allow, Refuse, Handoff, End }
public enum ProhibitedCategory { Unknown, Emergency, Medical, Financial, Payment, Authentication, Document, Legal, Penalty, Security, Commitment }
public sealed record GuardrailDecision(GuardrailAction Action, ProhibitedCategory Category, string Code)
{
    public static GuardrailDecision Unknown() => new(GuardrailAction.Handoff, ProhibitedCategory.Unknown, "UnknownIntent");
}
