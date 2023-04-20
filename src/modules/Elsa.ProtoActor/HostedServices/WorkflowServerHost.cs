using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;

namespace Elsa.ProtoActor.HostedServices;

/// <summary>
/// Starts the current member in the cluster.
/// </summary>
public class WorkflowServerHost : IHostedService
{
    private readonly ActorSystem _actorSystem;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowServerHost"/> class.
    /// </summary>
    /// <param name="actorSystem">The actor system to start.</param>
    public WorkflowServerHost(ActorSystem actorSystem) => _actorSystem = actorSystem;
    
    /// <summary>
    /// Starts the current member in the cluster.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task StartAsync(CancellationToken cancellationToken) => await _actorSystem.Cluster().StartMemberAsync();

    /// <summary>
    /// Stops the current member in the cluster.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task StopAsync(CancellationToken cancellationToken) => await _actorSystem.Cluster().ShutdownAsync();//Task.CompletedTask;
}