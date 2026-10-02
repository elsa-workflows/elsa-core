using Elsa.Caching.Distributed.ProtoActor.Actors;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;
using Proto.DependencyInjection;

namespace Elsa.Caching.Distributed.ProtoActor.HostedServices;

/// <summary>
/// Starts a member-local cache invalidator and subscribes it to the "change-token-signals" topic.
/// </summary>
[UsedImplicitly]
public class StartLocalCacheActor(Cluster cluster) : IHostedService
{
    private const string ActorName = "$memory-cache-invalidator";
    private PID? _actorPid;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var props = cluster.System.DI().PropsFor<MemoryCacheInvalidatorActor>();
        var actorPid = cluster.System.Root.SpawnNamedSystem(props, ActorName);

        try
        {
            await cluster.Subscribe(Topics.ChangeTokenSignals, actorPid, cancellationToken);
            _actorPid = actorPid;
        }
        catch
        {
            await cluster.System.Root.StopAsync(actorPid);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_actorPid == null)
            return;

        try
        {
            await cluster.Unsubscribe(Topics.ChangeTokenSignals, _actorPid, cancellationToken);
        }
        finally
        {
            await cluster.System.Root.StopAsync(_actorPid);
        }
    }
}