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
    public WorkflowServerHost(ActorSystem actorSystem) => _actorSystem = actorSystem;
    public async Task StartAsync(CancellationToken cancellationToken) => await _actorSystem.Cluster().StartMemberAsync();
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}