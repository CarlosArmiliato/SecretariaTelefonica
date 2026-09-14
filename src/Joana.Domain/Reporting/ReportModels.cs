using Joana.Domain.Primitives;
using Joana.Domain.Calls;

namespace Joana.Domain.Reporting;

public sealed record UntrustedTranscriptSegment(string Text);
public sealed record RedactedTranscriptSegment(string Text);
public sealed record RedactedCallReport(AttemptId AttemptId, DateTimeOffset StartedAtUtc, DateTimeOffset EndedAtUtc, string Destination, string Objective, string PreflightSummary, bool CarlosConfirmed, IReadOnlyList<string> People, IReadOnlyList<RedactedTranscriptSegment> Transcript, string Summary, ServiceProposal? Proposal, string Result, IReadOnlyList<string> Interventions, string PendingAction, IReadOnlyList<string> Errors);
public sealed record CallRecordManifest(int SchemaVersion, AttemptId AttemptId, DateTimeOffset EndedAtUtc, DateTimeOffset ExpiresAtUtc, IReadOnlyList<string> Targets);
