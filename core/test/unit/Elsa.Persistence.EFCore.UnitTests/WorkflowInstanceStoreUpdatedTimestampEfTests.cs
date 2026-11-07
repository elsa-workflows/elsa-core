using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Persistence.EFCore.UnitTests;

public class WorkflowInstanceStoreUpdatedTimestampEfTests
{
    [Theory(DisplayName = "UpdateUpdatedTimestampAsync changes only UpdatedAt, never the instance's tenant")]
    [InlineData(null, Tenant.DefaultTenantId)]
    [InlineData(Tenant.AgnosticTenantId, Tenant.DefaultTenantId)]
    [InlineData(Tenant.AgnosticTenantId, "t1")]
    [InlineData("t2", "t1")]
    public async Task UpdateUpdatedTimestamp_KeepsTenantId(string? instanceTenantId, string ambientTenantId)
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-instances-{Guid.NewGuid():N}.db");
        var tenantAccessor = new DefaultTenantAccessor();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ITenantAccessor>(tenantAccessor)
            .Configure<TenantsOptions>(options => options.IsEnabled = true)
            .AddScoped<IEntitySavingHandler, ApplyTenantId>()
            .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
            .AddSqliteEntityModelCreatingHandlers()
            .AddDbContextFactory<ManagementElsaDbContext>((_, builder) => builder.UseElsaSqlite(typeof(RuntimeDbContextFactory).Assembly, $"Data Source={databasePath};Default Timeout=30"))
            .Decorate<IDbContextFactory<ManagementElsaDbContext>, TenantAwareDbContextFactory<ManagementElsaDbContext>>()
            .AddScoped<EntityStore<ManagementElsaDbContext, WorkflowInstance>>()
            .BuildServiceProvider();
        var stampedAt = DateTimeOffset.Parse("2026-04-14T12:00:00Z");

        try
        {
            var dbContextFactory = services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>();

            await using (var dbContext = await dbContextFactory.CreateDbContextAsync())
            {
                await dbContext.Database.EnsureCreatedAsync();
                dbContext.WorkflowInstances.Add(new()
                {
                    Id = "i",
                    DefinitionId = "def",
                    DefinitionVersionId = "def-v1",
                    Version = 1,
                    TenantId = Tenant.AgnosticTenantId,
                    CreatedAt = DateTimeOffset.UnixEpoch,
                    UpdatedAt = DateTimeOffset.UnixEpoch
                });
                await dbContext.SaveChangesAsync();
                // Bypasses ApplyTenantId, which would turn a null tenant into the ambient one.
                await dbContext.WorkflowInstances.IgnoreQueryFilters().Where(x => x.Id == "i").ExecuteUpdateAsync(x => x.SetProperty(i => i.TenantId, instanceTenantId));
            }

            using (var scope = services.CreateScope())
            using (tenantAccessor.PushContext(new Tenant { Id = ambientTenantId, Name = ambientTenantId }))
            {
                var store = new EFCoreWorkflowInstanceStore(
                    scope.ServiceProvider.GetRequiredService<EntityStore<ManagementElsaDbContext, WorkflowInstance>>(),
                    Substitute.For<IWorkflowStateSerializer>(),
                    Substitute.For<ICompressionCodecResolver>(),
                    Microsoft.Extensions.Options.Options.Create(new ManagementOptions()),
                    NullLogger<EFCoreWorkflowInstanceStore>.Instance);

                await store.UpdateUpdatedTimestampAsync("i", stampedAt);
            }

            await using (var dbContext = await dbContextFactory.CreateDbContextAsync())
            {
                var instance = await dbContext.WorkflowInstances.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Id == "i");
                Assert.Equal(stampedAt, instance.UpdatedAt);
                Assert.Equal(instanceTenantId, instance.TenantId);
            }
        }
        finally
        {
            await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }
}
