using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Proto;

namespace Elsa.Caching.Distributed.ProtoActor.Actors;

internal class LocalCacheImpl(IContext context, IChangeTokenSignaler changeTokenSignaler) : LocalCacheBase(context)
{
    public override Task Start()
    {
        Context.System.EventStream.Subscribe<ProtoTriggerChangeTokenSignal>(OnTriggerChangeTokenSignalAsync);
        return Task.CompletedTask;
    }

    public override Task Stop()
    {
        Context.Poison(Context.Self);
        return Task.CompletedTask;
    }

    private async Task OnTriggerChangeTokenSignalAsync(ProtoTriggerChangeTokenSignal message)
    {
        await changeTokenSignaler.TriggerTokenAsync(message.Key);
    }
}