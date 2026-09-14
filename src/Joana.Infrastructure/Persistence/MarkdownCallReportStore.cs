using System.Text;
using System.Text.Json;
using Joana.Application.Ports;
using Joana.Domain.Reporting;

namespace Joana.Infrastructure.Persistence;
public sealed class MarkdownCallReportStore(string root) : ICallReportStore
{
    public async Task<string> WriteAsync(RedactedCallReport report, CancellationToken cancellationToken)
    {
        var saoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); var local = TimeZoneInfo.ConvertTime(report.StartedAtUtc, saoPaulo); var directory = Path.Combine(root, "records", "calls", local.ToString("yyyy-MM-dd")); Directory.CreateDirectory(directory);
        var baseName = local.ToString("HHmm") + "-destino"; var path = Path.Combine(directory, baseName + ".md"); var suffix = 0; while (File.Exists(path)) path = Path.Combine(directory, baseName + "-" + (++suffix).ToString("D2") + ".md");
        var content = new StringBuilder().AppendLine("# Registro de chamada").AppendLine($"- Inicio: {report.StartedAtUtc:O}").AppendLine($"- Fim: {report.EndedAtUtc:O}").AppendLine($"- Destino: {report.Destination}").AppendLine($"- Objetivo: {report.Objective}").AppendLine($"- Confirmacao: {report.CarlosConfirmed}").AppendLine($"- Resultado: {report.Result}").AppendLine("## Transcricao").AppendJoin('\n', report.Transcript.Select(x => x.Text)).ToString();
        await File.WriteAllTextAsync(path, content, cancellationToken); var manifest = new CallRecordManifest(1, report.AttemptId, report.EndedAtUtc, report.EndedAtUtc.AddDays(7), [Path.GetRelativePath(Path.Combine(root, "records", "calls"), path)]); await File.WriteAllTextAsync(path + ".manifest.json", JsonSerializer.Serialize(manifest), cancellationToken); return Path.GetRelativePath(root, path);
    }
}
