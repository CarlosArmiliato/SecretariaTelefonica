using Joana.Application.Orchestration;
using Joana.Application.Ports;
using Joana.Domain.Calls;
using Joana.Domain.Primitives;

namespace Joana.Acceptance.Cases.Authorization;

/// <summary>
/// T012 acceptance contract for preflight validation and immutable authorization bindings.
/// All values are synthetic; these cases never compose real adapters.
/// </summary>
public static class PreflightCases
{
    private static readonly AttemptId SyntheticAttemptId =
        new(new Guid("00000000-0000-0000-0000-000000000012"));

    public static Task<IReadOnlyList<string>> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var failures = new List<string>();
        var coordinator = new PreflightCoordinator(new FixedTimeSource());

        RejectsEveryMissingRequiredField(coordinator, failures);
        RequiresAnExplicitNoTestsDeclaration(coordinator, failures);
        CreatesAnImmutableRevisionSnapshot(coordinator, failures);
        InvalidatesAuthorizationWhenItsBindingChanges(coordinator, failures);

        return Task.FromResult<IReadOnlyList<string>>(failures);
    }

    private static void RejectsEveryMissingRequiredField(
        PreflightCoordinator coordinator,
        ICollection<string> failures)
    {
        var incompleteRequests = new (string Field, CallRequest Request)[]
        {
            (nameof(CallRequest.Company), CompleteRequest() with { Company = "" }),
            (nameof(CallRequest.PhoneNumber), CompleteRequest() with { PhoneNumber = "" }),
            (nameof(CallRequest.Objective), CompleteRequest() with { Objective = "" }),
            (nameof(CallRequest.Equipment), CompleteRequest() with { Equipment = "" }),
            (nameof(CallRequest.Problem), CompleteRequest() with { Problem = "" }),
            (nameof(CallRequest.Facts), CompleteRequest() with { Facts = [] }),
            (nameof(CallRequest.Tests), CompleteRequest() with { Tests = [] }),
            (nameof(CallRequest.PricePolicy), CompleteRequest() with { PricePolicy = null! }),
            (nameof(CallRequest.PricePolicy), CompleteRequest() with { PricePolicy = new PricePolicy("", false) }),
            (nameof(CallRequest.Availability), CompleteRequest() with { Availability = "" }),
            (nameof(CallRequest.Questions), CompleteRequest() with { Questions = [] }),
            (nameof(CallRequest.DisclosureGrants), CompleteRequest() with { DisclosureGrants = [] }),
            (nameof(CallRequest.DisclosureGrants), CompleteRequest() with
            {
                DisclosureGrants = [new DisclosureGrant("", false, false)]
            }),
            (nameof(CallRequest.SuccessCriteria), CompleteRequest() with { SuccessCriteria = [] }),
            (nameof(CallRequest.HandoffTriggers), CompleteRequest() with { HandoffTriggers = [] }),
        };

        foreach (var (field, request) in incompleteRequests)
        {
            var result = coordinator.Review(SyntheticAttemptId, request, out var revision);
            if (result.Succeeded || revision is not null)
            {
                failures.Add($"T012.required-field-{field}");
            }
        }
    }

    private static void RequiresAnExplicitNoTestsDeclaration(
        PreflightCoordinator coordinator,
        ICollection<string> failures)
    {
        var absent = coordinator.Review(
            SyntheticAttemptId,
            CompleteRequest() with { Tests = [] },
            out var absentRevision);
        var ambiguous = coordinator.Review(
            SyntheticAttemptId,
            CompleteRequest() with { Tests = [" "] },
            out var ambiguousRevision);
        var explicitNone = coordinator.Review(
            SyntheticAttemptId,
            CompleteRequest() with { Tests = ["Nenhum teste"] },
            out var explicitNoneRevision);

        if (absent.Succeeded || absentRevision is not null)
        {
            failures.Add("T012.tests-absence-is-not-explicit-none");
        }

        if (ambiguous.Succeeded || ambiguousRevision is not null)
        {
            failures.Add("T012.tests-blank-is-ambiguous");
        }

        if (!explicitNone.Succeeded || explicitNoneRevision is null)
        {
            failures.Add("T012.tests-explicit-none-is-accepted");
        }
    }

    private static void CreatesAnImmutableRevisionSnapshot(
        PreflightCoordinator coordinator,
        ICollection<string> failures)
    {
        var facts = new List<string> { "Fato sintético aprovado" };
        var tests = new List<string> { "Nenhum teste" };
        var questions = new List<string> { "Qual é o prazo?" };
        var successCriteria = new List<string> { "Receber proposta" };
        var handoffTriggers = new List<string> { "Dúvida" };
        var request = CompleteRequest() with
        {
            Facts = facts,
            Tests = tests,
            Questions = questions,
            SuccessCriteria = successCriteria,
            HandoffTriggers = handoffTriggers,
        };

        var reviewed = coordinator.Review(SyntheticAttemptId, request, out var revision);

        facts[0] = "Fato sintético alterado após a revisão";
        tests[0] = "Teste sintético inventado após a revisão";
        questions[0] = "Pergunta alterada após a revisão";
        successCriteria[0] = "Critério alterado após a revisão";
        handoffTriggers[0] = "Gatilho alterado após a revisão";

        if (!reviewed.Succeeded || revision is null ||
            revision.Request.Facts is not ["Fato sintético aprovado"] ||
            revision.Request.Tests is not ["Nenhum teste"] ||
            revision.Request.Questions is not ["Qual é o prazo?"] ||
            revision.Request.SuccessCriteria is not ["Receber proposta"] ||
            revision.Request.HandoffTriggers is not ["Dúvida"])
        {
            failures.Add("T012.revision-is-immutable-snapshot");
        }
    }

    private static void InvalidatesAuthorizationWhenItsBindingChanges(
        PreflightCoordinator coordinator,
        ICollection<string> failures)
    {
        var reviewed = coordinator.Review(SyntheticAttemptId, CompleteRequest(), out var original);
        if (!reviewed.Succeeded || original is null)
        {
            failures.Add("T012.baseline-review");
            return;
        }

        var changedRequests = new (string Field, CallRequest Request)[]
        {
            (nameof(CallRequest.Company), original.Request with { Company = "Assistência sintética alterada" }),
            (nameof(CallRequest.PhoneNumber), original.Request with { PhoneNumber = "00000000001" }),
            (nameof(CallRequest.Objective), original.Request with { Objective = "Solicitar avaliação sintética" }),
        };

        foreach (var (field, changedRequest) in changedRequests)
        {
            var authorization = new DialAuthorization(SyntheticAttemptId, original, true);
            var changedRevision = new PreflightRevision(
                original.Id,
                SyntheticAttemptId,
                changedRequest,
                original.CreatedAtUtc);

            if (authorization.IsValidFor(changedRevision, true) ||
                authorization.Status != DialAuthorizationStatus.Invalidated)
            {
                failures.Add($"T012.binding-change-invalidates-{field}");
            }
        }

        var refreshed = coordinator.Review(
            SyntheticAttemptId,
            CompleteRequest() with { Company = "Assistência sintética revisada" },
            out var refreshedRevision);
        var refreshedAuthorization = new DialAuthorization(SyntheticAttemptId, original, true);
        if (!refreshed.Succeeded || refreshedRevision is null ||
            refreshedRevision.Id == original.Id ||
            refreshedAuthorization.IsValidFor(refreshedRevision, true))
        {
            failures.Add("T012.changed-preflight-requires-new-authorization");
        }
    }

    private static CallRequest CompleteRequest() => new(
        "Assistência sintética",
        "00000000000",
        "Solicitar orçamento sintético",
        "Equipamento sintético",
        "Falha sintética",
        ["Fato sintético aprovado"],
        ["Nenhum teste"],
        new PricePolicy("Sem faixa informada", false),
        "Sem disponibilidade informada",
        ["Qual é o prazo?"],
        [new DisclosureGrant("referência-sintética", false, false)],
        ["Receber proposta"],
        ["Dúvida"]);

    private sealed class FixedTimeSource : ITimeSource
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
        public long MonotonicTimestamp => 0;
        public long MonotonicFrequency => 1;
    }
}
