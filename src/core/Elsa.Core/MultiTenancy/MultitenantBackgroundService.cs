using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Multitenancy
{
    public abstract class MultitenantBackgroundService : BackgroundService
    {
        private readonly ITenantStore _tenantStore;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public MultitenantBackgroundService(ITenantStore tenantStore, IServiceScopeFactory serviceScopeFactory)
        {
            _tenantStore = tenantStore;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var tenant in await _tenantStore.GetTenantsAsync())
            {
                using var scope = _serviceScopeFactory.CreateScopeForTenant(tenant);

                await ExecuteInternalAsync(scope.ServiceProvider, stoppingToken);
            }
        }

        protected abstract Task ExecuteInternalAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken);
    }
}
