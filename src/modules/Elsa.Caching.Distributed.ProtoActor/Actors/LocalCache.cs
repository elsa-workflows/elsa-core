using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;

namespace Elsa.Caching.Distributed.ProtoActor.Actors;

internal class LocalCache(IContext context, IChangeTokenSignalInvoker changeTokenSignaler) : LocalCacheBase(context)
{
    public override async Task OnStopped()
    {
        await Cluster.Unsubscribe(Topics.ChangeTokenSignals, Context.ClusterIdentity()!);
    }

    public override async Task OnReceive()
    {
        if (Context.Message is ProtoTriggerChangeTokenSignal triggerChangeTokenSignal)
        {
            await changeTokenSignaler.TriggerTokenAsync(triggerChangeTokenSignal.Key, Context.CancellationToken);
        }
    }
}