using Joana.Application.Ports;
namespace Joana.Simulators.Audio;
public sealed class SimulatedAudioControlAdapter : IAudioControlAdapter { public bool Fail { get; init; } public Task<AudioObservation> StopAndReturnHeadsetAsync(CancellationToken cancellationToken) => Task.FromResult(Fail ? new AudioObservation(false, false, false) : new AudioObservation(true, true, true)); }
