using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Proto.Cluster;
using Proto.Cluster.PubSub;

namespace Elsa.Caching.Distributed.ProtoActor.HostedServices;

/// Subscribes the LocalCacheActor to the "change-token-signals" topic.
[UsedImplicitly]
public class StartLocalCacheActor(Cluster cluster) : BackgroundService
{
    private static readonly string ActorName = Guid.NewGuid().ToString();
    private readonly ClusterIdentity _clusterIdentity = ClusterIdentity.Create(ActorName, LocalCacheActor.Kind);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await cluster.Subscribe(Topics.ChangeTokenSignals, _clusterIdentity, stoppingToken);
    }
}