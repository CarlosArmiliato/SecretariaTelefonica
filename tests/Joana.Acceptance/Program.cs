using Joana.Application.Orchestration;
using Joana.Application.Ports;
using Joana.Domain.Calls;
using Joana.Domain.Guardrails;
using Joana.Domain.Primitives;
using Joana.Domain.Reporting;
using Joana.Infrastructure.Persistence;
using Joana.Infrastructure.Redaction;
using Joana.Simulators.Audio;
using Joana.Simulators.Dialogue;
using Joana.Simulators.PhoneLink;

var suite = args.SkipWhile(x => x != "--suite").Skip(1).FirstOrDefault() ?? "all";
var failures = new List<string>();
void Check(bool condition, string name) { if (!condition) failures.Add(name); }
var clock = new TestTime();
var request = new CallRequest("Assistencia Sintetica", "11999990000", "Solicitar orcamento", "Equipamento", "Nao inicia", ["Nao liga"], ["Nenhum teste"], new PricePolicy("ate 100", false), "Segunda", ["Qual o prazo?"], [], ["Receber proposta"], ["Pagamento"]);
var attempt = new CallAttempt(AttemptId.New()); var preflight = new PreflightCoordinator(clock); var review = preflight.Review(attempt.Id, request, out var revision); Check(review.Succeeded && revision is not null, "preflight-valid");
if (revision is not null) { var phone = new SimulatedPhoneLinkAdapter(request.PhoneNumber); var storeRoot = Path.Combine(Path.GetTempPath(), "joana-tests-" + Guid.NewGuid()); var dial = new CallAttemptCoordinator(phone, new FileAttemptStore(storeRoot)); var auth = new DialAuthorization(attempt.Id, revision, true); Check(dial.ConfirmAndDialAsync(attempt, revision, auth, true, true, CancellationToken.None).GetAwaiter().GetResult().Succeeded, "authorized-dial"); Check(phone.EmissionCount == 1, "single-emission"); Check(!dial.ConfirmAndDialAsync(attempt, revision, auth, true, true, CancellationToken.None).GetAwaiter().GetResult().Succeeded, "no-retry"); }
var dialogue = new DialogueCoordinator(new SimulatedDialogueAdapter(), new DialoguePolicy()); var dialogueResult = dialogue.StartAsync("allowed", ["Fato aprovado"], ["Pergunta"], CancellationToken.None).GetAwaiter().GetResult(); Check(dialogueResult.Lines.FirstOrDefault() == DialogueCoordinator.Introduction, "literal-introduction"); Check(new DialoguePolicy().Evaluate("pagamento").Action == GuardrailAction.Handoff, "payment-handoff");
var handoff = new HandoffCoordinator(new SimulatedAudioControlAdapter(), clock).StartAsync("Unknown", CancellationToken.None).GetAwaiter().GetResult(); Check(handoff.VoiceStopped, "takeover-audio");
var redacted = new DeterministicRedactionService().Redact([new UntrustedTranscriptSegment("cpf 123.456.789-01")], out var requiresHandoff); Check(requiresHandoff && redacted.Single().Text.Contains("[DADO REDIGIDO]", StringComparison.Ordinal), "redaction");
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { suite, passed = failures.Count == 0, failures })); return failures.Count == 0 ? 0 : 1;

sealed class TestTime : ITimeSource { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; public long MonotonicTimestamp => 0; public long MonotonicFrequency => 1; }
