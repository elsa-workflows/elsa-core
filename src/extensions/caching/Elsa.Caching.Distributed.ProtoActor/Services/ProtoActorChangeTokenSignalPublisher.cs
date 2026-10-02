using Elsa.Caching.Distributed.Contracts;
using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Proto.Cluster;
using Proto.Cluster.PubSub;

namespace Elsa.Caching.Distributed.ProtoActor.Services;

public class ProtoActorChangeTokenSignalPublisher(Cluster cluster) : IChangeTokenSignalPublisher
{
    public async ValueTask PublishAsync(string key, CancellationToken cancellationToken = default)
    {
        var message = new ProtoTriggerChangeTokenSignal
        {
            Key = key
        };
        await cluster.Publisher().Publish(Topics.ChangeTokenSignals, message, cancellationToken);
    }
}