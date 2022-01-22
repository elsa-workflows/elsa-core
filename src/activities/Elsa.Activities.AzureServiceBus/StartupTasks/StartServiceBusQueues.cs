using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Open.Linq.AsyncExtensions;

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
            
            // Load all QueueMessageReceived bookmarks.
            var bookmarks = await bookmarkFinder.FindBookmarksByTypeAsync<QueueMessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

            // For each bookmark, start a worker.
            await _serviceBusQueuesStarter.CreateWorkersAsync(bookmarks, stoppingToken);
        }
    }
}