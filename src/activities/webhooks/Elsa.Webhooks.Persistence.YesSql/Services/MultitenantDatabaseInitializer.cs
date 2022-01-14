using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Webhooks.Persistence.YesSql.Services
{
    public class MultitenantDatabaseInitializer : DatabaseInitializer
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantDatabaseInitializer(IServiceScopeFactory scopeFactory, ITenantStore tenantStore) : base(scopeFactory) => _tenantStore = tenantStore;

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var tenant in _tenantStore.GetTenants())
            {
                var scope = _scopeFactory.CreateScopeForTenant(tenant);

                await ExecuteInternalAsync(scope);
            }
        }
    }
}
