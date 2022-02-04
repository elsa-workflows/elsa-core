using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Elsa.Services;
using Elsa.Webhooks.Persistence.YesSql.Data;
using Microsoft.Extensions.DependencyInjection;
using YesSql;

namespace Elsa.Webhooks.Persistence.YesSql.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantStore _tenantStore;

        public DatabaseInitializer(IServiceScopeFactory scopeFactory, ITenantStore tenantStore)
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

            var store = scope.ServiceProvider.GetRequiredService<IStore>();

            await store.InitializeCollectionAsync(CollectionNames.WebhookDefinitions);
        }
    }
}
