using Elsa.Common;
using JetBrains.Annotations;

namespace Elsa.Kafka.Tasks;

[UsedImplicitly]
public class StartConsumersStartupTask(IWorkerManager workerManager) : BackgroundTask
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await workerManager.UpdateWorkersAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        workerManager.StopWorkers();
        return Task.CompletedTask;
    }
}