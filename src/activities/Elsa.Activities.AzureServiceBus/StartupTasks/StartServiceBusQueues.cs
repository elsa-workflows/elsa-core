using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Activities.AzureServiceBus.StartupTasks
{
    public class StartServiceBusQueues : BackgroundService
    {
        private readonly IServiceBusQueuesStarter _serviceBusQueuesStarter;
        private readonly IServiceScopeFactory _scopeFactory;

        public StartServiceBusQueues(IServiceBusQueuesStarter serviceBusQueuesStarter, IServiceScopeFactory scopeFactory)
        {
            _serviceBusQueuesStarter = serviceBusQueuesStarter;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var bookmarkFinder = scope.ServiceProvider.GetRequiredService<IBookmarkFinder>();

            // Load bookmarks.
            var bookmarks = await bookmarkFinder.FindBookmarksByTypeAsync<QueueMessageReceivedBookmark>(cancellationToken: stoppingToken);

            // For each bookmark, start a worker.
            await _serviceBusQueuesStarter.CreateWorkersAsync(bookmarks, stoppingToken);

            // Load triggers.
            var triggerFinder = scope.ServiceProvider.GetRequiredService<ITriggerFinder>();
            var triggers = await triggerFinder.FindTriggersByTypeAsync<QueueMessageReceivedBookmark>(cancellationToken: stoppingToken);

            // For each trigger, start a worker.
            await _serviceBusQueuesStarter.CreateWorkersAsync(triggers, stoppingToken);
        }
    }
}