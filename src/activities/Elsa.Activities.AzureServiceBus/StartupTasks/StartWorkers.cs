using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Multitenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.AzureServiceBus.StartupTasks
{
    public class StartWorkers : MultitenantBackgroundService
    {
        public StartWorkers(IServiceScopeFactory scopeFactory, ITenantStore tenantStore) : base(tenantStore, scopeFactory) { }

        protected override async Task ExecuteInternalAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
        {
            var bookmarkFinder = serviceProvider.GetRequiredService<IBookmarkFinder>();
            var workerManager = serviceProvider.GetRequiredService<IWorkerManager>();

            // Load bookmarks.
            var bookmarks = await bookmarkFinder.FindBookmarksByTypeAsync<MessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

            // For each bookmark, start a worker.
            await workerManager.CreateWorkersAsync(bookmarks, serviceProvider, stoppingToken);

            // Load triggers.
            var triggerFinder = serviceProvider.GetRequiredService<ITriggerFinder>();
            var triggers = await triggerFinder.FindTriggersByTypeAsync<MessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

            // For each trigger, start a worker.
            await workerManager.CreateWorkersAsync(triggers, serviceProvider, stoppingToken);
        }
    }
}