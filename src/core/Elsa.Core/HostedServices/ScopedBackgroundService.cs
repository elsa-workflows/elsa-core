using System.Threading;
using System.Threading.Tasks;
using Elsa.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.HostedServices
{
    /// <summary>
    /// Executed the specified worker within a scoped-lifetime scope.
    /// </summary>
    public class ScopedBackgroundService<TWorker> : BackgroundService where TWorker:IScopedBackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantStore _tenantStore;

        public ScopedBackgroundService(IServiceScopeFactory scopeFactory, ITenantStore tenantStore)
        {
            _scopeFactory = scopeFactory;
            _tenantStore = tenantStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var tenant in await _tenantStore.GetTenantsAsync())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                var worker = (IScopedBackgroundService)ActivatorUtilities.GetServiceOrCreateInstance<TWorker>(scope.ServiceProvider);
                await worker.ExecuteAsync(stoppingToken);
            }
        }
    }
}