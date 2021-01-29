using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.StartupTasks
{
    /// <summary>
    /// Executes EF Core migrations.
    /// </summary>
    public class RunMigrations : IStartupTask
    {
        private readonly ElsaContext _dbContext;

        public RunMigrations(ElsaContext dbContext)
        {
            _dbContext = dbContext;
        }

        public int Order => 0;
        
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}