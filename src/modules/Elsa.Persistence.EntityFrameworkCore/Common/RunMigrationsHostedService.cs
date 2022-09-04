using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Elsa.Persistence.EntityFrameworkCore.Common
{
    /// <summary>
    /// Executes EF Core migrations using the specified <see cref="DbContext"/> type.
    /// </summary>
    public class RunMigrationsHostedService<TDbContext> : IHostedService where TDbContext : DbContext
    {
        private readonly IDbContextFactory<TDbContext> _dbContextFactory;
        public RunMigrationsHostedService(IDbContextFactory<TDbContext> dbContextFactoryFactory) => _dbContextFactory = dbContextFactoryFactory;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
            await dbContext.DisposeAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}