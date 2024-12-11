using Elsa.Common;
using JetBrains.Annotations;

namespace Elsa.Kafka.Tasks;

public class StartConsumersStartupTask(IWorkerManager workerManager, IWorkerTopicSubscriber workerTopicSubscriber) : BackgroundTask
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await workerManager.UpdateWorkersAsync(cancellationToken);
        await workerTopicSubscriber.UpdateTopicSubscriptionsAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        workerManager.StopWorkers();
        return Task.CompletedTask;
    }
}