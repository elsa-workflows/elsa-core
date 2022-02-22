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
    public class StartServiceBusTopics : BackgroundService
    {
        private readonly IServiceBusTopicsStarter _serviceBusTopicsStarter;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantStore _tenantStore;

        public StartServiceBusTopics(IServiceBusTopicsStarter serviceBusTopicsStarter, IServiceScopeFactory scopeFactory, ITenantStore tenantStore)
        {
            _serviceBusTopicsStarter = serviceBusTopicsStarter;
            _scopeFactory = scopeFactory;
            _tenantStore = tenantStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);
                var bookmarkFinder = scope.ServiceProvider.GetRequiredService<IBookmarkFinder>();

                // Load all TopicMessageReceivedBookmark bookmarks.
                var bookmarks = await bookmarkFinder.FindBookmarksByTypeAsync<TopicMessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

                // For each bookmark, start a worker.
                await _serviceBusTopicsStarter.CreateWorkersAsync(bookmarks, tenant, stoppingToken);
            }
        }
    }
}