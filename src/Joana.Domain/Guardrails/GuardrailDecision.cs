namespace Joana.Domain.Guardrails;

public enum GuardrailAction { Allow, Refuse, Handoff, End }
public enum ProhibitedCategory { Unknown, Emergency, Medical, Financial, Payment, Authentication, Document, Legal, Penalty, Security, Commitment }

public sealed record GuardrailDecision(GuardrailAction Action, ProhibitedCategory Category, string PolicyRule, IReadOnlySet<string> AllowedFactIds)
{
    public GuardrailDecision(GuardrailAction action, ProhibitedCategory category, string policyRule)
        : this(action, category, policyRule, EmptyFacts) { }

    public string Code => PolicyRule;
    public static GuardrailDecision Unknown() => new(GuardrailAction.Handoff, ProhibitedCategory.Unknown, "UnknownIntent", EmptyFacts);
    public static GuardrailDecision Allow(string policyRule, IEnumerable<string> allowedFactIds) => new(GuardrailAction.Allow, ProhibitedCategory.Unknown, policyRule, new HashSet<string>(allowedFactIds, StringComparer.Ordinal));
    public static GuardrailDecision Block(GuardrailAction action, ProhibitedCategory category, string policyRule)
    {
        if (action == GuardrailAction.Allow) throw new ArgumentOutOfRangeException(nameof(action));
        return new GuardrailDecision(action, category, policyRule, EmptyFacts);
    }
    private static IReadOnlySet<string> EmptyFacts { get; } = new HashSet<string>(StringComparer.Ordinal);
}