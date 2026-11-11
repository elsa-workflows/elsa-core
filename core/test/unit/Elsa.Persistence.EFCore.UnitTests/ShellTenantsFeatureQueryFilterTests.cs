using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.ShellFeatures;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

public class ShellTenantsFeatureQueryFilterTests
{
    [Fact(DisplayName = "Enabling the shell Tenants feature applies the EF tenant query filter")]
    public async Task ShellTenantsFeature_WhenEnabled_FiltersRowsOfOtherTenants()
    {
        // Arrange: the only tenancy setup is the shell Tenants feature itself.
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-shell-tenants-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ITenantAccessor>(new TestTenantAccessor("t1"))
            .AddScoped<IEntitySavingHandler, ApplyTenantId>()
            .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
            .AddSqliteEntityModelCreatingHandlers()
            .AddDbContextFactory<ManagementElsaDbContext>((_, builder) => builder.UseElsaSqlite(typeof(RuntimeDbContextFactory).Assembly, $"Data Source={databasePath};Default Timeout=30"));
        new TenantsFeature().ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        try
        {
            await using (var seedContext = await serviceProvider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                await seedContext.Database.EnsureCreatedAsync();
                seedContext.WorkflowInstances.Add(CreateInstance("i-t1", "t1"));
                seedContext.WorkflowInstances.Add(CreateInstance("i-t2", "t2"));
                await seedContext.SaveChangesAsync();
            }

            // Act
            await using var dbContext = await serviceProvider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
            var visibleIds = await dbContext.WorkflowInstances.Select(x => x.Id).ToListAsync();

            // Assert
            Assert.Equal(["i-t1"], visibleIds);
            Assert.True(dbContext.IsTenantFilteringEnabled);
        }
        finally
        {
            await serviceProvider.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    private static WorkflowInstance CreateInstance(string id, string tenantId) => new()
    {
        Id = id,
        DefinitionId = "def",
        DefinitionVersionId = "def-v1",
        Version = 1,
        TenantId = tenantId,
        CreatedAt = DateTimeOffset.UnixEpoch,
        UpdatedAt = DateTimeOffset.UnixEpoch
    };
}
