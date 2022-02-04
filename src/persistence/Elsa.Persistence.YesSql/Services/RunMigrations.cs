using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Elsa.Persistence.YesSql.Data;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.YesSql.Services
{
    public class RunMigrations : IStartupTask
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantStore _tenantStore;

        public RunMigrations(IServiceScopeFactory scopeFactory, ITenantStore tenantStore)
        {
            _scopeFactory = scopeFactory;
            _tenantStore = tenantStore;
        }

        public int Order => 0;
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var tenant in _tenantStore.GetTenants())
            {
                var scope = _scopeFactory.CreateScopeForTenant(tenant);

                await ExecuteInternalAsync(scope);
            }
        }

        private async Task ExecuteInternalAsync(IServiceScope? serviceScope = default)
        {
            using var scope = serviceScope ?? _scopeFactory.CreateScope();

            var dataMigrationManager = scope.ServiceProvider.GetRequiredService<IDataMigrationManager>();

            await dataMigrationManager.RunAllAsync();
        }
    }
}