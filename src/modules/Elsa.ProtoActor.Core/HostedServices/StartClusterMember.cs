using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;

namespace Elsa.ProtoActor.HostedServices;

/// Starts the current member in the cluster.
public class StartClusterMember(ActorSystem actorSystem) : IHostedService
{
    /// Starts the current member in the cluster.
    public Task StartAsync(CancellationToken cancellationToken) => actorSystem.Cluster().StartMemberAsync();

    /// Stops the current member in the cluster.
    public Task StopAsync(CancellationToken cancellationToken) => actorSystem.Cluster().ShutdownAsync();
}