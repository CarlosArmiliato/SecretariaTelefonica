using Joana.Domain.Primitives;

namespace Joana.Domain.Calls;

public sealed record PricePolicy(string Range, bool MayDisclose);
public sealed record DisclosureGrant(string FactId, bool IsModel, bool IsSerial);
public sealed record CallRequest(string Company, string PhoneNumber, string Objective, string Equipment, string Problem, IReadOnlyList<string> Facts, IReadOnlyList<string> Tests, PricePolicy PricePolicy, string Availability, IReadOnlyList<string> Questions, IReadOnlyList<DisclosureGrant> DisclosureGrants, IReadOnlyList<string> SuccessCriteria, IReadOnlyList<string> HandoffTriggers)
{
    public IReadOnlyList<string> Validate()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Company)) missing.Add("Company");
        if (string.IsNullOrWhiteSpace(PhoneNumber)) missing.Add("PhoneNumber");
        if (string.IsNullOrWhiteSpace(Objective)) missing.Add("Objective");
        if (string.IsNullOrWhiteSpace(Equipment)) missing.Add("Equipment");
        if (string.IsNullOrWhiteSpace(Problem)) missing.Add("Problem");
        if (Facts.Count == 0 || Tests.Count == 0 || Questions.Count == 0 || SuccessCriteria.Count == 0 || HandoffTriggers.Count == 0) missing.Add("RequiredLists");
        return missing;
    }
}

public sealed record PreflightRevision(PreflightRevisionId Id, AttemptId AttemptId, CallRequest Request, DateTimeOffset CreatedAtUtc);
