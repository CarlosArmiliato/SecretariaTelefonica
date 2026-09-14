using System.Text.Json;

namespace Joana.Infrastructure.Persistence;

public sealed class AtomicJsonFileStore
{
    public async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(directory);
        var lockPath = destination + ".lock";
        await using var exclusiveLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.WriteThrough);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, value, cancellationToken: cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(destination))
            {
                File.Replace(temporary, destination, null, true);
            }
            else
            {
                File.Move(temporary, destination, false);
            }
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}