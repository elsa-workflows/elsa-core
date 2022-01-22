using System;
using System.Threading;
using System.Threading.Tasks;
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var bookmarkFinder = scope.ServiceProvider.GetRequiredService<IBookmarkFinder>();
            
            // Load all bookmarks.
            var bookmarks = bookmarkFinder.FindBookmarksAsync()
            
            // Foreach bookmark, start a worker

            await _serviceBusQueuesStarter.CreateWorkersAsync();
        }
    }
}