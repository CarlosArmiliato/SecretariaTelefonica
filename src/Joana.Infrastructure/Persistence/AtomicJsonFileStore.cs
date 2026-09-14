using System.Text.Json;

namespace Joana.Infrastructure.Persistence;
public sealed class AtomicJsonFileStore
{
    public async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)) { await JsonSerializer.SerializeAsync(stream, value, cancellationToken: cancellationToken); await stream.FlushAsync(cancellationToken); stream.Flush(true); }
        File.Move(temporary, path, true);
    }
}
