using Elsa.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Elsa.Kafka.Tasks;

public class StartConsumersStartupTask(IWorkerManager workerManager, IWorkerTopicSubscriber workerTopicSubscriber) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await workerManager.UpdateWorkersAsync(cancellationToken);
        await workerTopicSubscriber.UpdateTopicSubscriptionsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        workerManager.StopWorkers();
        return Task.CompletedTask;
    }
}