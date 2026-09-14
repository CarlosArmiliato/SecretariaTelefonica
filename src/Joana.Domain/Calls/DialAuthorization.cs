using System.Security.Cryptography;
using System.Text;
using Joana.Domain.Primitives;

namespace Joana.Domain.Calls;

/// <summary>
/// A process-local, one-time authorization to emit a dial action.
/// Its binding digest is deliberately never persisted or exposed.
/// </summary>
public sealed class DialAuthorization
{
    private readonly object gate = new();
    private readonly byte[] bindingDigest;
    private DialAuthorizationStatus status;
    private string? invalidationReason;

    public DialAuthorization(AttemptId attemptId, PreflightRevision revision, bool supervisionPresent)
    {
        ArgumentNullException.ThrowIfNull(revision);

        AuthorizationId = Guid.NewGuid();
        AttemptId = attemptId;
        RevisionId = revision.Id;
        Company = revision.Request.Company;
        PhoneNumber = revision.Request.PhoneNumber;
        Objective = revision.Request.Objective;
        ConfirmedAtUtc = DateTimeOffset.UtcNow;
        SupervisionPresent = supervisionPresent;
        bindingDigest = CreateBindingDigest(attemptId, revision, supervisionPresent);
        status = revision.AttemptId == attemptId && supervisionPresent
            ? DialAuthorizationStatus.PendingUse
            : DialAuthorizationStatus.Invalidated;
        invalidationReason = status == DialAuthorizationStatus.Invalidated
            ? "AuthorizationBindingOrSupervisionInvalid"
            : null;
    }

    public Guid AuthorizationId { get; }

    public AttemptId AttemptId { get; }

    public PreflightRevisionId RevisionId { get; }

    public string Company { get; }

    public string PhoneNumber { get; }

    public string Objective { get; }

    public DateTimeOffset ConfirmedAtUtc { get; }

    public bool SupervisionPresent { get; }

    public DialAuthorizationStatus Status
    {
        get
        {
            lock (gate)
            {
                return status;
            }
        }
    }

    public bool Consumed => Status == DialAuthorizationStatus.Consumed;

    /// <summary>Contains a stable code only and never includes preflight content.</summary>
    public string? InvalidationReason
    {
        get
        {
            lock (gate)
            {
                return invalidationReason;
            }
        }
    }

    public bool IsValidFor(PreflightRevision revision, bool supervision)
    {
        ArgumentNullException.ThrowIfNull(revision);

        lock (gate)
        {
            if (status != DialAuthorizationStatus.PendingUse || !supervision || !SupervisionPresent ||
                revision.AttemptId != AttemptId || revision.Id != RevisionId ||
                !CryptographicOperations.FixedTimeEquals(
                    bindingDigest,
                    CreateBindingDigest(AttemptId, revision, SupervisionPresent)))
            {
                InvalidateUnsafe("AuthorizationBindingOrSupervisionInvalid");
                return false;
            }

            return true;
        }
    }

    /// <summary>Atomically consumes the authorization before the dial side effect.</summary>
    public OperationResult Consume(PreflightRevision revision, bool supervision)
    {
        if (!IsValidFor(revision, supervision))
        {
            return OperationResult.Fail("AuthorizationInvalid");
        }

        lock (gate)
        {
            // A competing consumer is rejected by this second status check.
            if (status != DialAuthorizationStatus.PendingUse)
            {
                return OperationResult.Fail("AuthorizationInvalid");
            }

            status = DialAuthorizationStatus.Consumed;
            invalidationReason = null;
            return OperationResult.Ok();
        }
    }

    /// <summary>Invalidates a pending authorization when the operator rejects or changes the review.</summary>
    public void Invalidate(string reasonCode = "AuthorizationInvalidated")
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("A stable invalidation code is required.", nameof(reasonCode));
        }

        lock (gate)
        {
            InvalidateUnsafe(reasonCode);
        }
    }

    /// <summary>Marks a pending authorization unusable after an in-memory process restart.</summary>
    public void InvalidateForRestart() => Invalidate("AuthorizationInvalidatedOnRestart");

    private void InvalidateUnsafe(string reasonCode)
    {
        if (status == DialAuthorizationStatus.PendingUse)
        {
            status = DialAuthorizationStatus.Invalidated;
            invalidationReason = reasonCode;
        }
    }

    private static byte[] CreateBindingDigest(
        AttemptId attemptId,
        PreflightRevision revision,
        bool supervisionPresent)
    {
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(digest, attemptId.Value.ToByteArray());
        Append(digest, revision.Id.Value.ToByteArray());
        Append(digest, revision.Request.Company);
        Append(digest, revision.Request.PhoneNumber);
        Append(digest, revision.Request.Objective);
        Append(digest, BitConverter.GetBytes(supervisionPresent));
        return digest.GetHashAndReset();
    }

    private static void Append(IncrementalHash digest, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        Append(digest, BitConverter.GetBytes(bytes.Length));
        Append(digest, bytes);
    }

    private static void Append(IncrementalHash digest, byte[] value)
    {
        digest.AppendData(BitConverter.GetBytes(value.Length));
        digest.AppendData(value);
    }
}

public enum DialAuthorizationStatus
{
    PendingUse,
    Consumed,
    Invalidated,
}
