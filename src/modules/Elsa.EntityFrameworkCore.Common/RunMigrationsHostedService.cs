using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore;

/// <summary>
/// Executes EF Core migrations using the specified <see cref="DbContext"/> type.
/// </summary>
[UsedImplicitly]
public class RunMigrationsHostedService<TDbContext>(IServiceScopeFactory scopeFactory) : MultitenantHostedService(scopeFactory) where TDbContext : DbContext
{
    /// <inheritdoc />
    protected override async Task StartAsync(TenantScope tenantScope, CancellationToken cancellationToken)
    {
        var dbContextFactory = tenantScope.ServiceProvider.GetRequiredService<IDbContextFactory<TDbContext>>();
        var tenantDbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await tenantDbContext.Database.MigrateAsync(cancellationToken);
    }
}