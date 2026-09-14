using System.Collections.ObjectModel;
using Joana.Domain.Primitives;

namespace Joana.Domain.Calls;

/// <summary>
/// Defines whether a price range may be mentioned during this preflight revision.
/// The range is information only; it never authorizes accepting a service or payment.
/// </summary>
public sealed record PricePolicy(string Range, bool MayDisclose);

/// <summary>
/// A disclosure permission scoped to one fact in one preflight revision.
/// Model and serial-number permissions are deliberately distinct.
/// </summary>
public sealed record DisclosureGrant(string FactId, bool IsModel, bool IsSerial)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(FactId) && !(IsModel && IsSerial);

    public bool IsModelOnly => IsModel && !IsSerial;

    public bool IsSerialOnly => IsSerial && !IsModel;
}

/// <summary>
/// The operator-supplied facts and limits for a single call review.
/// Collections are copied on construction so callers cannot mutate an approved request in place.
/// </summary>
public sealed record CallRequest
{
    public const string ExplicitNoTestsDeclaration = "Nenhum teste";

    public CallRequest(
        string company,
        string phoneNumber,
        string objective,
        string equipment,
        string problem,
        IReadOnlyList<string> facts,
        IReadOnlyList<string> tests,
        PricePolicy pricePolicy,
        string availability,
        IReadOnlyList<string> questions,
        IReadOnlyList<DisclosureGrant> disclosureGrants,
        IReadOnlyList<string> successCriteria,
        IReadOnlyList<string> handoffTriggers)
    {
        Company = company;
        PhoneNumber = phoneNumber;
        Objective = objective;
        Equipment = equipment;
        Problem = problem;
        Facts = Copy(facts);
        Tests = Copy(tests);
        PricePolicy = pricePolicy;
        Availability = availability;
        Questions = Copy(questions);
        DisclosureGrants = Copy(disclosureGrants);
        SuccessCriteria = Copy(successCriteria);
        HandoffTriggers = Copy(handoffTriggers);
    }

    public string Company { get; init; }

    public string PhoneNumber { get; init; }

    public string Objective { get; init; }

    public string Equipment { get; init; }

    public string Problem { get; init; }

    public IReadOnlyList<string> Facts { get; init; }

    public IReadOnlyList<string> Tests { get; init; }

    public PricePolicy PricePolicy { get; init; }

    public string Availability { get; init; }

    public IReadOnlyList<string> Questions { get; init; }

    public IReadOnlyList<DisclosureGrant> DisclosureGrants { get; init; }

    public IReadOnlyList<string> SuccessCriteria { get; init; }

    public IReadOnlyList<string> HandoffTriggers { get; init; }

    // Domain-language aliases keep the review contract explicit without duplicating state.
    public string CompanyName => Company;

    public string DialNumber => PhoneNumber;

    public string ProblemDescription => Problem;

    public IReadOnlyList<string> ApprovedFacts => Facts;

    public IReadOnlyList<string> ApprovedTestsAndErrors => Tests;

    public bool HasExplicitNoTestsDeclaration => HasOnlyExplicitNoTestsDeclaration(Tests);

    /// <summary>Returns stable validation codes without copying preflight content into diagnostics.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        AddIfBlank(errors, Company, nameof(Company));
        AddIfBlank(errors, PhoneNumber, nameof(PhoneNumber));
        AddIfBlank(errors, Objective, nameof(Objective));
        AddIfBlank(errors, Equipment, nameof(Equipment));
        AddIfBlank(errors, Problem, nameof(Problem));
        AddIfInvalidTextList(errors, Facts, nameof(Facts));
        AddIfInvalidTests(errors);
        AddIfInvalidTextList(errors, Questions, nameof(Questions));
        AddIfInvalidTextList(errors, SuccessCriteria, nameof(SuccessCriteria));
        AddIfInvalidTextList(errors, HandoffTriggers, nameof(HandoffTriggers));

        if (PricePolicy is null || string.IsNullOrWhiteSpace(PricePolicy.Range))
        {
            errors.Add(nameof(PricePolicy));
        }

        AddIfBlank(errors, Availability, nameof(Availability));
        AddIfInvalidDisclosureGrants(errors);

        return new ReadOnlyCollection<string>(errors);
    }

    /// <summary>Creates a content snapshot for an immutable review.</summary>
    public CallRequest Snapshot() => new(
        Company,
        PhoneNumber,
        Objective,
        Equipment,
        Problem,
        Facts,
        Tests,
        PricePolicy,
        Availability,
        Questions,
        DisclosureGrants,
        SuccessCriteria,
        HandoffTriggers);

    private static void AddIfBlank(ICollection<string> errors, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(field);
        }
    }

    private void AddIfInvalidTests(ICollection<string> errors)
    {
        AddIfInvalidTextList(errors, Tests, nameof(Tests));

        if (Tests is null || Tests.Count == 0)
        {
            return;
        }

        var hasExplicitNone = Tests.Any(IsNoTestsDeclaration);
        if (hasExplicitNone && !HasOnlyExplicitNoTestsDeclaration(Tests))
        {
            errors.Add(nameof(Tests));
        }
    }

    private void AddIfInvalidDisclosureGrants(ICollection<string> errors)
    {
        if (DisclosureGrants is null || DisclosureGrants.Count == 0)
        {
            errors.Add(nameof(DisclosureGrants));
            return;
        }

        var factIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grant in DisclosureGrants)
        {
            // Model and serial permissions must be two separate, unambiguous grants.
            if (grant is null || !grant.IsValid || !factIds.Add(grant.FactId))
            {
                errors.Add(nameof(DisclosureGrants));
                return;
            }
        }
    }

    private static void AddIfInvalidTextList(
        ICollection<string> errors,
        IReadOnlyList<string>? values,
        string field)
    {
        if (values is null || values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(field);
        }
    }

    private static bool HasOnlyExplicitNoTestsDeclaration(IReadOnlyList<string>? values) =>
        values is { Count: 1 } && IsNoTestsDeclaration(values[0]);

    private static bool IsNoTestsDeclaration(string? value) =>
        string.Equals(value?.Trim(), ExplicitNoTestsDeclaration, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value?.Trim(), "Nenhum", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T>? values) =>
        Array.AsReadOnly(values?.ToArray() ?? []);
}

/// <summary>A dated, immutable preflight snapshot bound to one call attempt.</summary>
public sealed record PreflightRevision
{
    public PreflightRevision(
        PreflightRevisionId id,
        AttemptId attemptId,
        CallRequest request,
        DateTimeOffset createdAtUtc)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("A preflight revision identifier is required.", nameof(id));
        }

        if (attemptId.Value == Guid.Empty)
        {
            throw new ArgumentException("An attempt identifier is required.", nameof(attemptId));
        }

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The revision timestamp must be UTC.", nameof(createdAtUtc));
        }

        Id = id;
        AttemptId = attemptId;
        Request = (request ?? throw new ArgumentNullException(nameof(request))).Snapshot();
        CreatedAtUtc = createdAtUtc;
    }

    public PreflightRevisionId Id { get; }

    public PreflightRevisionId PreflightRevisionId => Id;

    public AttemptId AttemptId { get; }

    public CallRequest Request { get; }

    public DateTimeOffset CreatedAtUtc { get; }
}
