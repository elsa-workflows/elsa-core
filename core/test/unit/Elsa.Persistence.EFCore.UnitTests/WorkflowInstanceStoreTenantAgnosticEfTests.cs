using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Persistence.EFCore.UnitTests;

public class WorkflowInstanceStoreTenantAgnosticEfTests
{
    private static readonly Dictionary<string, string> TenantById = new()
    {
        ["i-t1"] = "t1",
        ["i-t2"] = "t2",
        ["i-default"] = Tenant.DefaultTenantId,
        ["i-star"] = Tenant.AgnosticTenantId
    };

    [Theory(DisplayName = "Reads and counts honor TenantAgnostic, deletes stay tenant-scoped")]
    [InlineData(true, new[] { "i-default", "i-star", "i-t1", "i-t2" })]
    [InlineData(false, new[] { "i-star", "i-t1" })]
    public async Task ReadsFollowTenantAgnostic_DeleteStaysScoped(bool tenantAgnostic, string[] expectedIds)
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

        try
        {
            await using (var dbContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                await dbContext.Database.EnsureCreatedAsync();

                foreach (var (id, tenantId) in TenantById)
                {
                    dbContext.WorkflowInstances.Add(new()
                    {
                        Id = id,
                        DefinitionId = "def",
                        DefinitionVersionId = "def-v1",
                        Version = 1,
                        TenantId = tenantId,
                        CreatedAt = DateTimeOffset.UnixEpoch,
                        UpdatedAt = DateTimeOffset.UnixEpoch
                    });
                }

                await dbContext.SaveChangesAsync();
            }

            using var scope = services.CreateScope();
            using var _ = tenantAccessor.PushContext(new Tenant { Id = "t1", Name = "t1" });
            var store = new EFCoreWorkflowInstanceStore(
                scope.ServiceProvider.GetRequiredService<EntityStore<ManagementElsaDbContext, WorkflowInstance>>(),
                Substitute.For<IWorkflowStateSerializer>(),
                Substitute.For<ICompressionCodecResolver>(),
                Microsoft.Extensions.Options.Options.Create(new ManagementOptions()),
                NullLogger<EFCoreWorkflowInstanceStore>.Instance);

            var filter = new WorkflowInstanceFilter { TenantAgnostic = tenantAgnostic };
            var summaries = await store.EnumerateSummariesAsync(filter, 1).ToListAsync();

            Assert.Equal(expectedIds, summaries.Select(x => x.Id).Order());
            Assert.All(summaries, x => Assert.Equal(TenantById[x.Id], x.TenantId));
            Assert.Equal(expectedIds.Length, await store.CountAsync(filter));

            await store.DeleteAsync(filter);
            var remaining = await store.FindManyAsync(new() { TenantAgnostic = true });
            var remainingIds = remaining.Select(x => x.Id).ToList();
            Assert.Contains("i-default", remainingIds);
            Assert.Contains("i-t2", remainingIds);
            Assert.DoesNotContain("i-t1", remainingIds);
        }
        finally
        {
            await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }
}
