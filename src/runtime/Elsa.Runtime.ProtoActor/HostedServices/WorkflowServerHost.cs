using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;

namespace Elsa.Runtime.ProtoActor.HostedServices;

public class WorkflowServerHost : BackgroundService
{
    private readonly ActorSystem _actorSystem;

    public WorkflowServerHost(ActorSystem actorSystem)
    {
        _actorSystem = actorSystem;
    }
        
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _actorSystem.Cluster().StartMemberAsync();
    }
}