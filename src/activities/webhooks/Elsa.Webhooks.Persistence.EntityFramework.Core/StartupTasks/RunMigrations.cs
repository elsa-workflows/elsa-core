using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Webhooks.Persistence.EntityFramework.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core.StartupTasks
{
    /// <summary>
    /// Executes EF Core migrations.
    /// </summary>
    public class RunMigrations : IStartupTask
    {
        private readonly IWebhookContextFactory _dbContextFactory;

        public RunMigrations(IWebhookContextFactory dbContextFactoryFactory)
        {
            _dbContextFactory = dbContextFactoryFactory;
        }

        public int Order => 0;
        
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            await dbContext.Database.MigrateAsync(cancellationToken);
            await dbContext.DisposeAsync();
        }
    }
}