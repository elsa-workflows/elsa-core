using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;

namespace Elsa.ProtoActor.HostedServices;

/// <summary>
/// Starts the current member in the cluster.
/// </summary>
public class StartClusterMember(ActorSystem actorSystem) : IHostedService
{
    /// <summary>
    /// Starts the current member in the cluster.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken) => actorSystem.Cluster().StartMemberAsync();

    /// <summary>
    /// Stops the current member in the cluster.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken) => actorSystem.Cluster().ShutdownAsync();
}