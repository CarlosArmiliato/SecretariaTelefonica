using System.Text.Json;
using Joana.Domain.Reporting;

namespace Joana.Infrastructure.Persistence;
public sealed class CallRecordRetentionService(string root)
{
    public async Task<IReadOnlyList<string>> RemoveExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var callsRoot = Path.GetFullPath(Path.Combine(root, "records", "calls")); if (!Directory.Exists(callsRoot)) return [];
        var removed = new List<string>();
        foreach (var manifestPath in Directory.EnumerateFiles(callsRoot, "*.manifest.json", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(manifestPath) & FileAttributes.ReparsePoint) != 0) continue;
            CallRecordManifest? manifest; try { manifest = JsonSerializer.Deserialize<CallRecordManifest>(await File.ReadAllTextAsync(manifestPath, cancellationToken)); } catch (JsonException) { continue; }
            if (manifest is null || nowUtc < manifest.ExpiresAtUtc) continue;
            foreach (var relative in manifest.Targets)
            { var target = Path.GetFullPath(Path.Combine(callsRoot, relative)); if (!target.StartsWith(callsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || File.Exists(target) && (File.GetAttributes(target) & FileAttributes.ReparsePoint) != 0) continue; if (File.Exists(target)) { File.Delete(target); removed.Add(relative); } }
            if (File.Exists(manifestPath)) File.Delete(manifestPath);
        }
        return removed;
    }
}
