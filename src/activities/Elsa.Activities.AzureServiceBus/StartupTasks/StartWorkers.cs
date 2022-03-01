using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Multitenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.AzureServiceBus.StartupTasks
{
    public class StartWorkers : BackgroundService
    {
        private readonly IWorkerManager _workerManager;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantStore _tenantStore;

        public StartWorkers(IWorkerManager workerManager, IServiceScopeFactory scopeFactory, ITenantStore tenantStore)
        {
            _workerManager = workerManager;
            _scopeFactory = scopeFactory;
            _tenantStore = tenantStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var tenant in await _tenantStore.GetTenantsAsync())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);
                var bookmarkFinder = scope.ServiceProvider.GetRequiredService<IBookmarkFinder>();

                // Load bookmarks.
                var bookmarks = await bookmarkFinder.FindBookmarksByTypeAsync<MessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

                // For each bookmark, start a worker.
                await _workerManager.CreateWorkersAsync(bookmarks, tenant, stoppingToken);

                // Load triggers.
                var triggerFinder = scope.ServiceProvider.GetRequiredService<ITriggerFinder>();
                var triggers = await triggerFinder.FindTriggersByTypeAsync<MessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

                // For each trigger, start a worker.
                await _workerManager.CreateWorkersAsync(triggers, tenant, stoppingToken);
            }
        }
    }
}