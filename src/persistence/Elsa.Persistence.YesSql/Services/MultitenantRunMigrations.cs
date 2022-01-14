using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.YesSql.Services
{
    public class MultitenantRunMigrations : RunMigrations
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantRunMigrations(ITenantStore tenantStore, IServiceScopeFactory scopeFactory) : base(scopeFactory) => _tenantStore = tenantStore;

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