using System.Text.Json;
using Joana.Application.Orchestration;
using Joana.Application.Ports;
using Joana.Domain.Calls;
using Joana.Domain.Primitives;
using Joana.Infrastructure.Persistence;
using Joana.Simulators.PhoneLink;

namespace Joana.Acceptance.Support;

/// <summary>
/// Deterministic simulation-only regression runner. Results contain stable
/// suite and assertion codes only, never exception text or caller data.
/// </summary>
public sealed class ScenarioRunner
{
    private static readonly IReadOnlySet<string> SupportedSuites = new HashSet<string>(StringComparer.Ordinal)
    {
        "all", "authorization", "guardrails", "idempotency", "handoff", "reporting", "retention"
    };

    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<ScenarioRunResult> RunAsync(string[] arguments, CancellationToken cancellationToken)
    {
        var selection = Parse(arguments);
        if (!selection.IsValid)
        {
            return ScenarioRunResult.Failed("all", selection.ErrorCode!);
        }

        var assertions = new ScenarioAssertions();
        var temporaryRoot = CreateSyntheticRoot();
        try
        {
            // Additional suites are registered as their stories are implemented.
            // A valid, not-yet-implemented selection remains a safe no-op.
            if (selection.Suite is "all" or "authorization")
            {
                await RunAuthorizationAsync(assertions, temporaryRoot, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            assertions.Fail("runner.cancelled");
        }
        catch
        {
            // Do not expose exception text, paths, or stack traces in the JSON result.
            assertions.Fail("runner.unexpected-failure");
        }
        finally
        {
            TryDeleteSyntheticRoot(temporaryRoot);
        }

        return new ScenarioRunResult(selection.Suite!, assertions.Passed, assertions.Failures);
    }

    public static bool IsSimulation(string mode) => string.Equals(mode, "Simulation", StringComparison.OrdinalIgnoreCase);

    private static async Task RunAuthorizationAsync(ScenarioAssertions assertions, string temporaryRoot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        assertions.True(IsSimulation("Simulation"), "authorization.simulation-mode");
        assertions.True(!IsSimulation("Production"), "authorization.production-mode-rejected");

        var coordinator = new PreflightCoordinator(new FixedTimeSource());
        var attempt = new CallAttempt(AttemptId.New());
        var complete = CreateSyntheticRequest();
        var accepted = coordinator.Review(attempt.Id, complete, out var revision);
        assertions.True(accepted.Succeeded && revision is not null, "authorization.complete-preflight");
        if (revision is null) return;
        assertions.True(attempt.MoveTo(AttemptState.PreflightReady).Succeeded, "authorization.preflight-state");
        assertions.True(attempt.MoveTo(AttemptState.AwaitingConfirmation).Succeeded, "authorization.confirmation-state");
        assertions.True(attempt.MoveTo(AttemptState.Authorized).Succeeded, "authorization.authorized-state");

        var phone = new SimulatedPhoneLinkAdapter(complete.PhoneNumber);
        var dial = new CallAttemptCoordinator(phone, new FileAttemptStore(temporaryRoot));
        var authorization = new DialAuthorization(attempt.Id, revision, true);
        var dialResult = await dial.ConfirmAndDialAsync(attempt, revision, authorization, true, true, cancellationToken);
        assertions.True(dialResult.Succeeded, "authorization.authorized-dial");
        assertions.True(phone.EmissionCount == 1 && attempt.DialEmissionCount == 1, "authorization.single-emission");
        var replay = await dial.ConfirmAndDialAsync(attempt, revision, authorization, true, true, cancellationToken);
        assertions.True(!replay.Succeeded && phone.EmissionCount == 1, "authorization.replay-blocked");

        var refused = coordinator.Review(attempt.Id, CreateSyntheticRequest(string.Empty), out var missingRevision);
        assertions.True(!refused.Succeeded && missingRevision is null, "authorization.incomplete-preflight-blocked");
    }

    private static CallRequest CreateSyntheticRequest(string company = "Assistencia sintetica") => new(
        company, "00000000000", "Solicitar proposta sintetica", "Equipamento sintetico", "Falha sintetica",
        ["Fato sintetico"], ["Nenhum teste"], new PricePolicy("Faixa sintetica", false), "Horario sintetico",
        ["Qual o prazo?"], [], ["Receber proposta"], ["Transferir em caso de duvida"]);

    private static Selection Parse(IReadOnlyList<string> arguments)
    {
        var suite = "all";
        var suiteSeen = false;
        var modeSeen = false;
        for (var index = 0; index < arguments.Count; index++)
        {
            switch (arguments[index])
            {
                case "--suite" when !suiteSeen && index + 1 < arguments.Count:
                    suite = arguments[++index].ToLowerInvariant();
                    suiteSeen = true;
                    break;
                case "--mode" when !modeSeen && index + 1 < arguments.Count:
                    modeSeen = true;
                    if (!IsSimulation(arguments[++index])) return Selection.Invalid("runner.production-mode-disabled");
                    break;
                default:
                    return Selection.Invalid("runner.invalid-arguments");
            }
        }

        return SupportedSuites.Contains(suite) ? Selection.Valid(suite) : Selection.Invalid("runner.unsupported-suite");
    }

    private static string CreateSyntheticRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "joana-acceptance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void TryDeleteSyntheticRoot(string root)
    {
        try { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
        catch { /* cleanup cannot leak a filesystem path through the result */ }
    }

    private sealed record Selection(string? Suite, string? ErrorCode)
    {
        public bool IsValid => Suite is not null;
        public static Selection Valid(string suite) => new(suite, null);
        public static Selection Invalid(string errorCode) => new(null, errorCode);
    }

    private sealed class FixedTimeSource : ITimeSource
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
        public long MonotonicTimestamp => 0;
        public long MonotonicFrequency => 1;
    }

    private sealed class ScenarioAssertions
    {
        private readonly List<string> failures = [];
        public IReadOnlyList<string> Failures => failures;
        public bool Passed => failures.Count == 0;
        public void True(bool condition, string failureCode)
        {
            if (!condition && !failures.Contains(failureCode, StringComparer.Ordinal)) failures.Add(failureCode);
        }
        public void Fail(string failureCode) => failures.Add(failureCode);
    }
}

public sealed record ScenarioRunResult(string Suite, bool Passed, IReadOnlyList<string> Failures)
{
    public static ScenarioRunResult Failed(string suite, string failureCode) => new(suite, false, [failureCode]);
}
