using Elsa.Caching.Distributed.Contracts;
using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Proto.Cluster;

namespace Elsa.Caching.Distributed.ProtoActor.Services;

public class ProtoActorChangeTokenSignalPublisher(Cluster cluster) : IChangeTokenSignalPublisher
{
    public ValueTask PublishAsync(string key, CancellationToken cancellationToken = default)
    {
        var message = new ProtoTriggerChangeTokenSignal
        {
            Key = key
        };
        cluster.System.EventStream.Publish(message);
        return ValueTask.CompletedTask;
    }
}