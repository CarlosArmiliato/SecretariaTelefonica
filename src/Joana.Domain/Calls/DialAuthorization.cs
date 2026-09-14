using Joana.Domain.Primitives;

namespace Joana.Domain.Calls;

public sealed class DialAuthorization
{
    public DialAuthorization(AttemptId attemptId, PreflightRevision revision, bool supervisionPresent)
    { AttemptId = attemptId; RevisionId = revision.Id; Company = revision.Request.Company; PhoneNumber = revision.Request.PhoneNumber; Objective = revision.Request.Objective; SupervisionPresent = supervisionPresent; }
    public AttemptId AttemptId { get; }
    public PreflightRevisionId RevisionId { get; }
    public string Company { get; }
    public string PhoneNumber { get; }
    public string Objective { get; }
    public bool SupervisionPresent { get; }
    public bool Consumed { get; private set; }
    public bool IsValidFor(PreflightRevision revision, bool supervision) => !Consumed && supervision && SupervisionPresent && revision.Id == RevisionId && revision.Request.Company == Company && revision.Request.PhoneNumber == PhoneNumber && revision.Request.Objective == Objective;
    public OperationResult Consume(PreflightRevision revision, bool supervision) { if (!IsValidFor(revision, supervision)) return OperationResult.Fail("AuthorizationInvalid"); Consumed = true; return OperationResult.Ok(); }
}
