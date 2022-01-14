using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.YesSql.Data;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.YesSql.Services
{
    public class RunMigrations : IStartupTask
    {
        protected readonly IServiceScopeFactory _scopeFactory;

        public RunMigrations(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public int Order => 0;
        public virtual async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await ExecuteInternalAsync();
        }

        protected async Task ExecuteInternalAsync(IServiceScope? serviceScope = default)
        {
            using var scope = serviceScope ?? _scopeFactory.CreateScope();

            var dataMigrationManager = scope.ServiceProvider.GetRequiredService<IDataMigrationManager>();

            await dataMigrationManager.RunAllAsync();
        }
    }
}