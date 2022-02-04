using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Elsa.Services;
using Elsa.Webhooks.Persistence.EntityFramework.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core.StartupTasks
{
    /// <summary>
    /// Executes EF Core migrations.
    /// </summary>
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

                await ExecuteInternalAsync(cancellationToken, scope);
            }
        }

        private async Task ExecuteInternalAsync(CancellationToken cancellationToken = default, IServiceScope? serviceScope = default)
        {
            using var scope = serviceScope ?? _scopeFactory.CreateScope();

            var dbContextFactory = scope.ServiceProvider.GetRequiredService<IWebhookContextFactory>();

            await using var dbContext = dbContextFactory.CreateDbContext();
            await dbContext.Database.MigrateAsync(cancellationToken);
            await dbContext.DisposeAsync();
        }
    }
}