using Elsa.Common.Multitenancy;
using Elsa.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.EntityFrameworkCore;

/// <summary>
/// Executes EF Core migrations using the specified <see cref="DbContext"/> type.
/// </summary>
[UsedImplicitly]
public class RunMigrationsHostedService<TDbContext>(IServiceScopeFactory scopeFactory, ITenantScopeFactory tenantScopeFactory) : IHostedService
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var tenantsProvider = scope.ServiceProvider.GetRequiredService<ITenantsProvider>();
        var tenants = (await tenantsProvider.ListAsync(cancellationToken)).ToList();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TDbContext>>();

        if (tenants.Any())
        {
            foreach (var tenant in tenants)
            {
                using (tenantScopeFactory.CreateScope(tenant))
                {
                    var tenantDbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                    await tenantDbContext.Database.MigrateAsync(cancellationToken);
                }
            }
        }
        else
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}