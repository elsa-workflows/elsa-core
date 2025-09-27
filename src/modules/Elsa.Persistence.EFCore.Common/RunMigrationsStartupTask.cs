using Elsa.Common;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore;

/// <summary>
/// Executes EF Core migrations using the specified <see cref="DbContext"/> type.
/// </summary>
[UsedImplicitly]
[SingleNodeTask]
[Order(-100)]
public class RunMigrationsStartupTask<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory, IOptions<MigrationOptions> options) : IStartupTask where TDbContext : DbContext
{
    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        options.Value.RunMigrations.TryGetValue(typeof(TDbContext), out bool shouldRunMigrations);
     
        if (!shouldRunMigrations)
            return;

        var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}