using Joana.Domain.Calls;
using Joana.Domain.Primitives;
using Joana.Application.Ports;

namespace Joana.Application.Orchestration;

/// <summary>
/// Validates an operator-provided preflight before it can be presented for dial confirmation.
/// This coordinator never initiates a call and deliberately returns only stable diagnostic codes.
/// </summary>
public sealed class PreflightCoordinator
{
    private const string InvalidPreflightCode = "PreflightIncomplete";
    private readonly ITimeSource? time;

    public PreflightCoordinator(ITimeSource time)
    {
        this.time = time;
    }

    /// <summary>
    /// Produces a fresh, immutable review only when every preflight field is resolved.
    /// An invalid identifier, unavailable clock, malformed request, or non-UTC timestamp is treated
    /// as ambiguous and blocks review without disclosing request content.
    /// </summary>
    public OperationResult Review(AttemptId attemptId, CallRequest? request, out PreflightRevision? revision)
    {
        revision = null;

        if (attemptId.Value == Guid.Empty || request is null || time is null)
        {
            return OperationResult.Fail(InvalidPreflightCode);
        }

        try
        {
            // Validate the same defensive snapshot that will be bound to the review. This prevents
            // a caller-owned collection from changing between validation and revision creation.
            var snapshot = request.Snapshot();
            if (snapshot.Validate().Count != 0)
            {
                return OperationResult.Fail(InvalidPreflightCode);
            }

            var createdAtUtc = time.UtcNow;
            if (createdAtUtc.Offset != TimeSpan.Zero)
            {
                return OperationResult.Fail(InvalidPreflightCode);
            }

            // PreflightRevision snapshots the request. Generate the identifier here rather than
            // caching reviews so every accepted review has a distinct authorization binding.
            revision = new PreflightRevision(PreflightRevisionId.New(), attemptId, snapshot, createdAtUtc);
            return OperationResult.Ok();
        }
        catch (Exception)
        {
            // A malformed caller-supplied collection or clock must not permit progression.
            revision = null;
            return OperationResult.Fail(InvalidPreflightCode);
        }
    }
}
