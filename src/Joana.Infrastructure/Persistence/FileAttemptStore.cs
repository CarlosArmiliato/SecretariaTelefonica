using System.Text.Json;
using System.Text.Json.Serialization;
using Joana.Application.Ports;
using Joana.Domain.Calls;
using Joana.Domain.Primitives;

namespace Joana.Infrastructure.Persistence;

/// <summary>
/// Persists the one-way marker written before a dial side effect. Existing,
/// incomplete, unreadable, or incompatible markers are always ambiguous.
/// </summary>
public sealed class FileAttemptStore(string root) : IAttemptStore
{
    private const int SchemaVersion = 1;

    public async Task<OperationResult> CreateIntentAsync(CallAttempt attempt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var path = GetIntentPath(attempt.Id);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // CreateNew is the cross-process idempotency gate. Never replace a
            // marker which may have been left incomplete by a crash.
            await using var stream = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough);

            var intent = new IntentSnapshot(
                SchemaVersion,
                attempt.Id.ToString(),
                attempt.Sequence,
                attempt.State.ToString(),
                DateTimeOffset.UtcNow);

            await JsonSerializer.SerializeAsync(stream, intent, cancellationToken: cancellationToken);
            await stream.FlushAsync(cancellationToken);
            stream.Flush(flushToDisk: true);
            return OperationResult.Ok();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // A failure before the intent is durable prevents the caller from
            // reaching the dial action. This store never retries the write.
            return OperationResult.Fail("IntentPersistenceFailed");
        }
    }

    public async Task<bool> HasAmbiguousStateAsync(AttemptId attemptId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var path = GetIntentPath(attemptId);
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            _ = await JsonSerializer.DeserializeAsync<IntentSnapshot>(stream, cancellationToken: cancellationToken);

            // Even a valid prior intent is terminal for automatic dialing. A
            // null or incompatible document likewise remains blocked; it is
            // retained for reconciliation instead of being retried or erased.
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        catch (Exception)
        {
            // Truncated, locked, or unreadable state is uncertain and blocks.
            return true;
        }
    }

    private string GetIntentPath(AttemptId attemptId) => Path.Combine(root, "state", "attempts", $"{attemptId}.intent.json");

    // Closed DTO: only anti-replay fields; never phone, name, text, or hash.
    private sealed record IntentSnapshot(
        [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
        [property: JsonPropertyName("attemptId")] string AttemptId,
        [property: JsonPropertyName("sequence")] int Sequence,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("createdAtUtc")] DateTimeOffset CreatedAtUtc);
}
