using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Proto;

namespace Elsa.Caching.Distributed.ProtoActor.Actors;

internal class MemoryCacheInvalidatorActor(IChangeTokenSignalInvoker changeTokenSignaler) : IActor
{
    public async Task ReceiveAsync(IContext context)
    {
        if (context.Message is ProtoTriggerChangeTokenSignal triggerChangeTokenSignal)
        {
            await changeTokenSignaler.TriggerTokenAsync(triggerChangeTokenSignal.Key, context.CancellationToken);
        }
    }
}