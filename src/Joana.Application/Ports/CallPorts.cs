using Joana.Domain.Calls;
using Joana.Domain.Primitives;
using Joana.Domain.Reporting;

namespace Joana.Application.Ports;

public enum PhoneCallPhase { Unknown, Idle, Dialling, Connected, Ending }

public sealed record PhoneObservation(string Application, bool Focused, string VisibleDestination, bool CallActive, string Token)
{
    public string Window { get; init; } = string.Empty;
    public PhoneCallPhase Phase { get; init; } = PhoneCallPhase.Unknown;
}

public enum DialResult { Issued, Rejected, Unknown }

public interface IPhoneLinkAdapter
{
    Task<PhoneObservation> ObserveAsync(AttemptId attemptId, CancellationToken cancellationToken);
    Task<DialResult> DialOnceAsync(AttemptId attemptId, PhoneObservation observation, CancellationToken cancellationToken);
    Task<DialResult> DialOnceAsync(AttemptId attemptId, CallAttempt persistedIntent, PhoneObservation observation, CancellationToken cancellationToken)
        => Task.FromResult(DialResult.Rejected);
    Task<OperationResult> EndAsync(AttemptId attemptId, PhoneObservation observation, CancellationToken cancellationToken);
}

public sealed record AudioObservation(bool VoiceStopped, bool HeadsetInputReturned, bool HeadsetOutputReturned)
{
    public bool IsSafeForHumanTakeover => VoiceStopped && HeadsetInputReturned && HeadsetOutputReturned;
}

public interface IAudioControlAdapter
{
    Task<AudioObservation> StopAndReturnHeadsetAsync(CancellationToken cancellationToken);
    Task<AudioObservation> StopAndReturnHeadsetAsync(AttemptId attemptId, CancellationToken cancellationToken)
        => Task.FromResult(new AudioObservation(false, false, false));
}

public interface IDialogueAdapter
{
    Task<IReadOnlyList<string>> StartAsync(IReadOnlyList<string> allowedFacts, IReadOnlyList<string> questions, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> StartAsync(AttemptId attemptId, IReadOnlyList<string> allowedFacts, IReadOnlyList<string> questions, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([]);
}

public interface ITranscriptionAdapter
{
    IAsyncEnumerable<UntrustedTranscriptSegment> TranscribeAsync(CancellationToken cancellationToken);
    async IAsyncEnumerable<UntrustedTranscriptSegment> TranscribeAsync(AttemptId attemptId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield break;
    }
}

public interface IRedactionService
{
    IReadOnlyList<RedactedTranscriptSegment> Redact(IReadOnlyList<UntrustedTranscriptSegment> segments, out bool requiresHandoff);
    IReadOnlyList<RedactedTranscriptSegment> Redact(AttemptId attemptId, IReadOnlyList<UntrustedTranscriptSegment> segments, out bool requiresHandoff)
        => Redact(segments, out requiresHandoff);
}

public interface IAttemptStore
{
    Task<OperationResult> CreateIntentAsync(CallAttempt attempt, CancellationToken cancellationToken);
    Task<bool> HasAmbiguousStateAsync(AttemptId attemptId, CancellationToken cancellationToken);
}

public interface ICallReportStore
{
    Task<string> WriteAsync(RedactedCallReport report, CancellationToken cancellationToken);
    Task<string> WriteAsync(AttemptId attemptId, RedactedCallReport report, CancellationToken cancellationToken)
        => attemptId == report.AttemptId
            ? WriteAsync(report, cancellationToken)
            : Task.FromException<string>(new InvalidOperationException("CallReportAttemptMismatch"));
}