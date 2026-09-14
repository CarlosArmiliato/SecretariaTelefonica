using Joana.Application.Ports;
using Joana.Domain.Calls;
using Joana.Domain.Primitives;

namespace Joana.Application.Orchestration;
public sealed class CallAttemptCoordinator(IPhoneLinkAdapter phone, IAttemptStore attempts)
{
    public async Task<OperationResult> ConfirmAndDialAsync(CallAttempt attempt, PreflightRevision revision, DialAuthorization authorization, bool simulationMode, bool supervisionPresent, CancellationToken cancellationToken)
    {
        if (!simulationMode || !supervisionPresent || await attempts.HasAmbiguousStateAsync(attempt.Id, cancellationToken)) return OperationResult.Fail("DialBlocked");
        var observed = await phone.ObserveAsync(attempt.Id, cancellationToken);
        if (observed.Application != "PhoneLink" || !observed.Focused || observed.CallActive || observed.VisibleDestination != revision.Request.PhoneNumber) return OperationResult.Fail("ObservationInvalid");
        if (!authorization.Consume(revision, supervisionPresent).Succeeded) return OperationResult.Fail("AuthorizationInvalid");
        if (!attempt.MoveTo(AttemptState.IntentPersisted).Succeeded) return OperationResult.Fail("StateBlocked");
        var persisted = await attempts.CreateIntentAsync(attempt, cancellationToken);
        if (!persisted.Succeeded) return persisted;
        var dial = await phone.DialOnceAsync(attempt.Id, observed, cancellationToken);
        return dial switch { DialResult.Issued => attempt.MoveTo(AttemptState.DialIssued), DialResult.Rejected => attempt.MoveTo(AttemptState.Failed), _ => attempt.MoveTo(AttemptState.Uncertain) };
    }
}
