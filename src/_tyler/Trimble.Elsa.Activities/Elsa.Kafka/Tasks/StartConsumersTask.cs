using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Kafka.Tasks;

public class StartConsumersStartupTask(IWorkerManager workerManager, IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await workerManager.UpdateWorkersAsync(cancellationToken);
        
        using var scope = scopeFactory.CreateScope();
        var workerTopicSubscriber = scope.ServiceProvider.GetRequiredService<IWorkerTopicSubscriber>();
        await workerTopicSubscriber.UpdateTopicSubscriptionsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        workerManager.StopWorkers();
        return Task.CompletedTask;
    }
}