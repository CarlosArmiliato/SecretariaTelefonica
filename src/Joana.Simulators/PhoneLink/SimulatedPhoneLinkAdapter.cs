using Joana.Application.Ports;
using Joana.Domain.Primitives;

namespace Joana.Simulators.PhoneLink;
public sealed class SimulatedPhoneLinkAdapter(string destination) : IPhoneLinkAdapter
{
    private string? token; public int EmissionCount { get; private set; } public bool ReturnUnknown { get; init; } public bool Focused { get; set; } = true;
    public Task<PhoneObservation> ObserveAsync(AttemptId attemptId, CancellationToken cancellationToken) { token = Guid.NewGuid().ToString("N"); return Task.FromResult(new PhoneObservation("PhoneLink", Focused, destination, false, token)); }
    public Task<DialResult> DialOnceAsync(AttemptId attemptId, PhoneObservation observation, CancellationToken cancellationToken) { if (token is null || token != observation.Token) return Task.FromResult(DialResult.Rejected); token = null; EmissionCount++; return Task.FromResult(ReturnUnknown ? DialResult.Unknown : DialResult.Issued); }
    public Task<OperationResult> EndAsync(AttemptId attemptId, PhoneObservation observation, CancellationToken cancellationToken) => Task.FromResult(OperationResult.Ok());
}
