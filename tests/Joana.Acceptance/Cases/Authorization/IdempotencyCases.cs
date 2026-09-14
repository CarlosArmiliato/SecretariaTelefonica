using Joana.Application.Orchestration;
using Joana.Application.Ports;
using Joana.Domain.Calls;
using Joana.Domain.Primitives;
using Joana.Infrastructure.Persistence;
using Joana.Simulators.PhoneLink;

namespace Joana.Acceptance.Cases.Authorization;

/// <summary>
/// T013 acceptance cases. These cases use only synthetic data and simulated
/// adapters. Each failure code identifies one idempotency guarantee.
/// </summary>
public static class IdempotencyCases
{
    public static async Task<IReadOnlyList<string>> RunAsync(string temporaryRoot, CancellationToken cancellationToken)
    {
        var failures = new List<string>();

        await ConfirmationIsSingleUseAsync(temporaryRoot, failures, cancellationToken);
        await DoubleClickEmitsOnceAsync(temporaryRoot, failures, cancellationToken);
        await ReplayDoesNotEmitAgainAsync(temporaryRoot, failures, cancellationToken);
        await TwoInstancesEmitOnceAsync(temporaryRoot, failures, cancellationToken);
        await CrashBeforeIntentDoesNotDialAsync(failures, cancellationToken);
        await DiskFullDoesNotDialAsync(failures, cancellationToken);
        await TruncatedIntentBlocksDialAsync(temporaryRoot, failures, cancellationToken);
        await CrashAfterIntentRequiresReconciliationAsync(temporaryRoot, failures, cancellationToken);
        await LostReturnRequiresReconciliationAsync(temporaryRoot, failures, cancellationToken);
        await RestartDoesNotRetryAsync(temporaryRoot, failures, cancellationToken);

        return failures;
    }

    private static async Task ConfirmationIsSingleUseAsync(
        string root,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        const string failureCode = "T013.confirmation-single-use";
        var (attempt, revision) = CreateAuthorizedAttempt();
        var authorization = new DialAuthorization(attempt.Id, revision, supervisionPresent: true);
        var phone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber);
        var coordinator = CreateCoordinator(phone, root, "confirmation-single-use");

        var first = await coordinator.ConfirmAndDialAsync(attempt, revision, authorization, true, true, cancellationToken);
        var second = await coordinator.ConfirmAndDialAsync(attempt, revision, authorization, true, true, cancellationToken);

        if (!first.Succeeded || second.Succeeded || phone.EmissionCount != 1 || attempt.DialEmissionCount != 1)
        {
            failures.Add(failureCode);
        }
    }

    private static async Task DoubleClickEmitsOnceAsync(
        string root,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        const string failureCode = "T013.double-click-emits-once";
        var (attempt, revision) = CreateAuthorizedAttempt();
        var authorization = new DialAuthorization(attempt.Id, revision, supervisionPresent: true);
        var phone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber);
        var coordinator = CreateCoordinator(phone, root, "double-click");

        var results = await Task.WhenAll(
            coordinator.ConfirmAndDialAsync(attempt, revision, authorization, true, true, cancellationToken),
            coordinator.ConfirmAndDialAsync(attempt, revision, authorization, true, true, cancellationToken));

        if (results.Count(result => result.Succeeded) != 1 || phone.EmissionCount != 1 || attempt.DialEmissionCount != 1)
        {
            failures.Add(failureCode);
        }
    }

    private static async Task ReplayDoesNotEmitAgainAsync(
        string root,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        const string failureCode = "T013.replay-does-not-emit-again";
        var (attempt, revision) = CreateAuthorizedAttempt();
        var authorization = new DialAuthorization(attempt.Id, revision, supervisionPresent: true);
        var phone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber);
        var coordinator = CreateCoordinator(phone, root, "replay");

        var first = await coordinator.ConfirmAndDialAsync(attempt, revision, authorization, true, true, cancellationToken);
        var replay = await coordinator.ConfirmAndDialAsync(attempt, revision, authorization, true, true, cancellationToken);

        if (!first.Succeeded || replay.Succeeded || phone.EmissionCount != 1)
        {
            failures.Add(failureCode);
        }
    }

    private static async Task TwoInstancesEmitOnceAsync(
        string root,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        const string failureCode = "T013.two-instances";
        var (firstAttempt, revision) = CreateAuthorizedAttempt();
        var secondAttempt = CreateAuthorizedAttempt(firstAttempt.Id, revision).Attempt;
        var firstAuthorization = new DialAuthorization(firstAttempt.Id, revision, supervisionPresent: true);
        var secondAuthorization = new DialAuthorization(secondAttempt.Id, revision, supervisionPresent: true);
        var firstPhone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber);
        var secondPhone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber);
        var stateRoot = Path.Combine(root, "two-instances");
        var first = new CallAttemptCoordinator(firstPhone, new FileAttemptStore(stateRoot));
        var second = new CallAttemptCoordinator(secondPhone, new FileAttemptStore(stateRoot));

        var results = await Task.WhenAll(
            first.ConfirmAndDialAsync(firstAttempt, revision, firstAuthorization, true, true, cancellationToken),
            second.ConfirmAndDialAsync(secondAttempt, revision, secondAuthorization, true, true, cancellationToken));

        var emissions = firstPhone.EmissionCount + secondPhone.EmissionCount;
        if (results.Count(result => result.Succeeded) != 1 || emissions != 1)
        {
            failures.Add(failureCode);
        }
    }

    private static async Task CrashBeforeIntentDoesNotDialAsync(
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        const string failureCode = "T013.crash-before-intent-does-not-dial";
        var (attempt, revision) = CreateAuthorizedAttempt();
        var phone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber);
        var coordinator = new CallAttemptCoordinator(phone, new CrashBeforeIntentStore());

        var result = await coordinator.ConfirmAndDialAsync(
            attempt,
            revision,
            new DialAuthorization(attempt.Id, revision, supervisionPresent: true),
            simulationMode: true,
            supervisionPresent: true,
            cancellationToken);

        if (result.Succeeded || phone.EmissionCount != 0 || attempt.DialEmissionCount != 0)
        {
            failures.Add(failureCode);
        }
    }

    private static async Task DiskFullDoesNotDialAsync(
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        const string failureCode = "T013.disk-full-does-not-dial";
        var (attempt, revision) = CreateAuthorizedAttempt();
        var phone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber);
        var coordinator = new CallAttemptCoordinator(phone, new DiskFullAttemptStore());

        var result = await coordinator.ConfirmAndDialAsync(
            attempt,
            revision,
            new DialAuthorization(attempt.Id, revision, supervisionPresent: true),
            simulationMode: true,
            supervisionPresent: true,
            cancellationToken);

        if (result.Succeeded || phone.EmissionCount != 0 || attempt.DialEmissionCount != 0)
        {
            failures.Add(failureCode);
        }
    }

    private static async Task TruncatedIntentBlocksDialAsync(
        string root,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        const string failureCode = "T013.truncated-intent-blocks-dial";
        var (attempt, revision) = CreateAuthorizedAttempt();
        var intentDirectory = Path.Combine(root, "truncated", "state", "attempts");
        Directory.CreateDirectory(intentDirectory);
        await File.WriteAllTextAsync(Path.Combine(intentDirectory, $"{attempt.Id}.intent.json"), "{", cancellationToken);

        var phone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber);
        var coordinator = CreateCoordinator(phone, root, "truncated");
        var result = await coordinator.ConfirmAndDialAsync(
            attempt,
            revision,
            new DialAuthorization(attempt.Id, revision, supervisionPresent: true),
            simulationMode: true,
            supervisionPresent: true,
            cancellationToken);

        if (result.Succeeded || phone.EmissionCount != 0 || attempt.DialEmissionCount != 0)
        {
            failures.Add(failureCode);
        }
    }

    private static async Task CrashAfterIntentRequiresReconciliationAsync(
        string root,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        const string failureCode = "T013.crash-after-intent-requires-reconciliation";
        var (attempt, revision) = CreateAuthorizedAttempt();
        var stateRoot = Path.Combine(root, "crash-after-intent");
        var phone = new ThrowAfterEmissionPhoneAdapter(revision.Request.PhoneNumber);
        var coordinator = new CallAttemptCoordinator(phone, new FileAttemptStore(stateRoot));
        var escaped = false;
        OperationResult result = OperationResult.Ok("Unexpected");

        try
        {
            result = await coordinator.ConfirmAndDialAsync(
                attempt,
                revision,
                new DialAuthorization(attempt.Id, revision, supervisionPresent: true),
                simulationMode: true,
                supervisionPresent: true,
                cancellationToken);
        }
        catch
        {
            escaped = true;
        }

        var restartedAttempt = CreateAuthorizedAttempt(attempt.Id, revision).Attempt;
        var restartedPhone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber);
        var restarted = new CallAttemptCoordinator(restartedPhone, new FileAttemptStore(stateRoot));
        var retry = await restarted.ConfirmAndDialAsync(
            restartedAttempt,
            revision,
            new DialAuthorization(restartedAttempt.Id, revision, supervisionPresent: true),
            simulationMode: true,
            supervisionPresent: true,
            cancellationToken);

        if (escaped || result.Succeeded || attempt.State != AttemptState.Reconciling ||
            phone.EmissionCount != 1 || retry.Succeeded || restartedPhone.EmissionCount != 0)
        {
            failures.Add(failureCode);
        }
    }

    private static async Task LostReturnRequiresReconciliationAsync(
        string root,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        const string failureCode = "T013.lost-return-requires-reconciliation";
        var (attempt, revision) = CreateAuthorizedAttempt();
        var phone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber) { ReturnUnknown = true };
        var coordinator = CreateCoordinator(phone, root, "lost-return");
        var authorization = new DialAuthorization(attempt.Id, revision, supervisionPresent: true);

        var result = await coordinator.ConfirmAndDialAsync(attempt, revision, authorization, true, true, cancellationToken);
        var retry = await coordinator.ConfirmAndDialAsync(attempt, revision, authorization, true, true, cancellationToken);

        if (result.Succeeded || retry.Succeeded || attempt.State != AttemptState.Reconciling || phone.EmissionCount != 1)
        {
            failures.Add(failureCode);
        }
    }

    private static async Task RestartDoesNotRetryAsync(
        string root,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        const string failureCode = "T013.restart-without-retry";
        var (attempt, revision) = CreateAuthorizedAttempt();
        var stateRoot = Path.Combine(root, "restart");
        var firstPhone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber) { ReturnUnknown = true };
        var first = new CallAttemptCoordinator(firstPhone, new FileAttemptStore(stateRoot));

        var firstResult = await first.ConfirmAndDialAsync(
            attempt,
            revision,
            new DialAuthorization(attempt.Id, revision, supervisionPresent: true),
            simulationMode: true,
            supervisionPresent: true,
            cancellationToken);

        var restartedAttempt = CreateAuthorizedAttempt(attempt.Id, revision).Attempt;
        var restartedPhone = new SimulatedPhoneLinkAdapter(revision.Request.PhoneNumber);
        var restarted = new CallAttemptCoordinator(restartedPhone, new FileAttemptStore(stateRoot));
        var retry = await restarted.ConfirmAndDialAsync(
            restartedAttempt,
            revision,
            new DialAuthorization(restartedAttempt.Id, revision, supervisionPresent: true),
            simulationMode: true,
            supervisionPresent: true,
            cancellationToken);

        if (firstResult.Succeeded || retry.Succeeded || restartedPhone.EmissionCount != 0)
        {
            failures.Add(failureCode);
        }
    }

    private static CallAttemptCoordinator CreateCoordinator(
        IPhoneLinkAdapter phone,
        string root,
        string caseName) =>
        new(phone, new FileAttemptStore(Path.Combine(root, caseName)));

    private static (CallAttempt Attempt, PreflightRevision Revision) CreateAuthorizedAttempt(
        AttemptId? id = null,
        PreflightRevision? existingRevision = null)
    {
        var request = existingRevision?.Request ?? new CallRequest(
            "Assistência sintética",
            "00000000000",
            "Solicitar orçamento sintético",
            "Equipamento sintético",
            "Falha sintética",
            ["Fato sintético"],
            ["Nenhum teste relatado"],
            new PricePolicy("Faixa sintética", mayDisclose: false),
            "Horário sintético",
            ["Qual é o prazo?"],
            [new DisclosureGrant("referência-sintética", isModel: false, isSerial: false)],
            ["Receber proposta"],
            ["Dúvida"]);
        var attempt = new CallAttempt(id ?? AttemptId.New());
        attempt.MoveTo(AttemptState.PreflightReady);
        attempt.MoveTo(AttemptState.AwaitingConfirmation);
        attempt.MoveTo(AttemptState.Authorized);
        return (attempt, existingRevision ?? new PreflightRevision(
            PreflightRevisionId.New(),
            attempt.Id,
            request,
            DateTimeOffset.UnixEpoch));
    }

    private sealed class CrashBeforeIntentStore : IAttemptStore
    {
        public Task<OperationResult> CreateIntentAsync(CallAttempt attempt, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult.Fail("SyntheticCrashBeforeIntent"));

        public Task<bool> HasAmbiguousStateAsync(AttemptId attemptId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class DiskFullAttemptStore : IAttemptStore
    {
        public Task<OperationResult> CreateIntentAsync(CallAttempt attempt, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult.Fail("SyntheticDiskFull"));

        public Task<bool> HasAmbiguousStateAsync(AttemptId attemptId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class ThrowAfterEmissionPhoneAdapter(string destination) : IPhoneLinkAdapter
    {
        private string? observationToken;

        public int EmissionCount { get; private set; }

        public Task<PhoneObservation> ObserveAsync(AttemptId attemptId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            observationToken = Guid.NewGuid().ToString("N");
            return Task.FromResult(new PhoneObservation("PhoneLink", true, destination, false, observationToken));
        }

        public Task<DialResult> DialOnceAsync(
            AttemptId attemptId,
            PhoneObservation observation,
            CancellationToken cancellationToken) =>
            DialAndCrashAsync(observation, cancellationToken);

        public Task<DialResult> DialOnceAsync(
            AttemptId attemptId,
            CallAttempt persistedIntent,
            PhoneObservation observation,
            CancellationToken cancellationToken) =>
            DialAndCrashAsync(observation, cancellationToken);

        public Task<OperationResult> EndAsync(
            AttemptId attemptId,
            PhoneObservation observation,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult.Ok());

        private Task<DialResult> DialAndCrashAsync(PhoneObservation observation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (observationToken is null || observationToken != observation.Token)
            {
                return Task.FromResult(DialResult.Rejected);
            }

            observationToken = null;
            EmissionCount++;
            throw new IOException("SyntheticCrashAfterEmission");
        }
    }
}
