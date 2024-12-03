using Elsa.Common;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore;

/// <summary>
/// Executes EF Core migrations using the specified <see cref="DbContext"/> type.
/// </summary>
[UsedImplicitly]
[SingleNodeTask]
[Order(-100)]
public class RunMigrationsStartupTask<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory) : IStartupTask where TDbContext : DbContext
{
    /// <inheritdoc /
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var tenantDbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await tenantDbContext.Database.MigrateAsync(cancellationToken);
    }
}