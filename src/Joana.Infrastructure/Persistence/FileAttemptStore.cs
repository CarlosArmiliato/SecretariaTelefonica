using System.Text.Json;
using Joana.Application.Ports;
using Joana.Domain.Calls;
using Joana.Domain.Primitives;

namespace Joana.Infrastructure.Persistence;
public sealed class FileAttemptStore(string root) : IAttemptStore
{
    public async Task<OperationResult> CreateIntentAsync(CallAttempt attempt, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(root, "state", "attempts"); Directory.CreateDirectory(directory); var path = Path.Combine(directory, attempt.Id + ".intent.json");
        try { await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough); await JsonSerializer.SerializeAsync(stream, new { schemaVersion = 1, attemptId = attempt.Id.ToString(), sequence = attempt.Sequence, state = attempt.State.ToString() }, cancellationToken: cancellationToken); await stream.FlushAsync(cancellationToken); stream.Flush(true); return OperationResult.Ok(); }
        catch (IOException) { return OperationResult.Fail("IntentAlreadyExistsOrStorageFailure"); }
    }
    public Task<bool> HasAmbiguousStateAsync(AttemptId attemptId, CancellationToken cancellationToken)
    { var path = Path.Combine(root, "state", "attempts", attemptId + ".intent.json"); return Task.FromResult(File.Exists(path)); }
}
