using System.Text.RegularExpressions;
using Joana.Application.Ports;
using Joana.Domain.Reporting;

namespace Joana.Infrastructure.Redaction;
public sealed partial class DeterministicRedactionService : IRedactionService
{
    [GeneratedRegex(@"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b|\b(?:senha|token|cpf|endereço|endereco|serie|serial)\b[^\n]*", RegexOptions.IgnoreCase)] private static partial Regex Sensitive();
    public IReadOnlyList<RedactedTranscriptSegment> Redact(IReadOnlyList<UntrustedTranscriptSegment> segments, out bool requiresHandoff)
    { var combined = string.Join("\n", segments.Select(x => x.Text)); requiresHandoff = Sensitive().IsMatch(combined); return [new(Sensitive().Replace(combined, "[DADO REDIGIDO]"))]; }
}
