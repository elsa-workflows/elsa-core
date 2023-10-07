using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Elsa.EntityFrameworkCore.Common;

/// <summary>
/// Executes EF Core migrations using the specified <see cref="DbContext"/> type.
/// </summary>
public class RunMigrationsHostedService<TDbContext> : IHostedService where TDbContext : DbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RunMigrationsHostedService{TDbContext}"/> class.
    /// </summary>
    public RunMigrationsHostedService(IDbContextFactory<TDbContext> dbContextFactoryFactory) => _dbContextFactory = dbContextFactoryFactory;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        await dbContext.DisposeAsync();
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}