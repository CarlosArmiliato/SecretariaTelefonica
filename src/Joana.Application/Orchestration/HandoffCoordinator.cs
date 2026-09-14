using Joana.Application.Ports;
using Joana.Domain.Handoff;

namespace Joana.Application.Orchestration;
public sealed class HandoffCoordinator(IAudioControlAdapter audio, ITimeSource time)
{
    public const string SafeEndStatement = "Cheguei ao limite do que estou autorizada a tratar. Vou encerrar a chamada para que Carlos possa continuar depois.";
    public async Task<HumanHandoff> StartAsync(string reason, CancellationToken cancellationToken) { var handoff = new HumanHandoff(reason, time.UtcNow, time.MonotonicTimestamp); var observed = await audio.StopAndReturnHeadsetAsync(cancellationToken); if (observed.VoiceStopped && observed.HeadsetInputReturned && observed.HeadsetOutputReturned) handoff.MarkVoiceStopped(); else handoff.Fail(); return handoff; }
    public bool Confirm(HumanHandoff handoff, Joana.Domain.Primitives.HandoffId id) => handoff.Confirm(id, time.MonotonicTimestamp, time.MonotonicFrequency);
    public bool Timeout(HumanHandoff handoff) => handoff.Expire(time.MonotonicTimestamp, time.MonotonicFrequency);
}
