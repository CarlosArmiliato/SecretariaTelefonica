using Joana.Domain.Calls;
using Joana.Domain.Primitives;
using Joana.Domain.Reporting;

namespace Joana.Application.Ports;

public sealed record PhoneObservation(string Application, bool Focused, string VisibleDestination, bool CallActive, string Token);
public enum DialResult { Issued, Rejected, Unknown }
public interface IPhoneLinkAdapter { Task<PhoneObservation> ObserveAsync(AttemptId attemptId, CancellationToken cancellationToken); Task<DialResult> DialOnceAsync(AttemptId attemptId, PhoneObservation observation, CancellationToken cancellationToken); Task<OperationResult> EndAsync(AttemptId attemptId, PhoneObservation observation, CancellationToken cancellationToken); }
public sealed record AudioObservation(bool VoiceStopped, bool HeadsetInputReturned, bool HeadsetOutputReturned);
public interface IAudioControlAdapter { Task<AudioObservation> StopAndReturnHeadsetAsync(CancellationToken cancellationToken); }
public interface IDialogueAdapter { Task<IReadOnlyList<string>> StartAsync(IReadOnlyList<string> allowedFacts, IReadOnlyList<string> questions, CancellationToken cancellationToken); }
public interface ITranscriptionAdapter { IAsyncEnumerable<UntrustedTranscriptSegment> TranscribeAsync(CancellationToken cancellationToken); }
public interface IRedactionService { IReadOnlyList<RedactedTranscriptSegment> Redact(IReadOnlyList<UntrustedTranscriptSegment> segments, out bool requiresHandoff); }
public interface IAttemptStore { Task<OperationResult> CreateIntentAsync(CallAttempt attempt, CancellationToken cancellationToken); Task<bool> HasAmbiguousStateAsync(AttemptId attemptId, CancellationToken cancellationToken); }
public interface ICallReportStore { Task<string> WriteAsync(RedactedCallReport report, CancellationToken cancellationToken); }
