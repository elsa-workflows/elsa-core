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
[SingleNodeTask(SingleNodeTaskScope.Host)]
[Order(-100)]
public class RunMigrationsStartupTask<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory, IOptions<MigrationOptions> options, MigratedDatabaseRegistry? migratedDatabaseRegistry = null) : IStartupTask where TDbContext : DbContext
{
    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        options.Value.RunMigrations.TryGetValue(typeof(TDbContext), out bool shouldRunMigrations);

        if (!shouldRunMigrations)
            return;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var connectionString = dbContext.Database.IsRelational() ? dbContext.Database.GetConnectionString() : null;

        if (migratedDatabaseRegistry?.TryClaim(typeof(TDbContext), connectionString) == false)
            return;

        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        catch
        {
            migratedDatabaseRegistry?.Release(typeof(TDbContext), connectionString);
            throw;
        }
    }
}
