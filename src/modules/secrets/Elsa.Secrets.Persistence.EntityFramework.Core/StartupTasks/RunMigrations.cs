using System.Threading;
using System.Threading.Tasks;
using Elsa.Secrets.Persistence.EntityFramework.Core.Services;
using Elsa.Services;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFramework.Core.StartupTasks
{
    public class RunMigrations : IStartupTask
    {
        private readonly ISecretsContextFactory _dbContextFactory;

        public RunMigrations(ISecretsContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
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
