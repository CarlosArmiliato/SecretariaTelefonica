using Joana.Domain.Calls;
using Joana.Domain.Primitives;
using Joana.Application.Ports;

namespace Joana.Application.Orchestration;
public sealed class PreflightCoordinator(ITimeSource time)
{
    public OperationResult Review(AttemptId attemptId, CallRequest request, out PreflightRevision? revision)
    { var errors = request.Validate(); revision = errors.Count == 0 ? new(PreflightRevisionId.New(), attemptId, request, time.UtcNow) : null; return errors.Count == 0 ? OperationResult.Ok() : OperationResult.Fail("PreflightIncomplete"); }
}
