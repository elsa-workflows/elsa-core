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

    [Fact(DisplayName = "Without tenant context reads include default, null and agnostic rows but exclude named tenants")]
    public async Task ReadsWithoutTenantContext_IncludeDefaultNullAndAgnostic()
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-no-context-read-{Guid.NewGuid():N}.db");
        var tenantAccessor = new DefaultTenantAccessor();
        var services = BuildServices(databasePath, tenantAccessor);

        try
        {
            await using (var dbContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                await dbContext.Database.EnsureCreatedAsync();

                // Explicit TenantId values are preserved when no tenant context exists.
                foreach (var (id, tenantId) in new[]
                {
                    ("default-row", (string?)Tenant.DefaultTenantId),
                    ("null-row", (string?)null),
                    ("star-row", Tenant.AgnosticTenantId),
                    ("t1-row", "t1")
                })
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

            await using (var readContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                Assert.Equal(Tenant.DefaultTenantId, readContext.TenantId);
                Assert.False(readContext.HasTenantContext);

                var ids = await readContext.WorkflowInstances
                    .Select(x => x.Id)
                    .OrderBy(x => x)
                    .ToListAsync();

                Assert.Equal(new[] { "default-row", "null-row", "star-row" }, ids);
            }
        }
        finally
        {
            await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact(DisplayName = "Without tenant context writes stamp missing TenantId with the default tenant and keep explicit ids")]
    public async Task WritesWithoutTenantContext_StampDefaultAndKeepExplicit()
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-no-context-write-{Guid.NewGuid():N}.db");
        var tenantAccessor = new DefaultTenantAccessor();
        var services = BuildServices(databasePath, tenantAccessor);

        try
        {
            await using (var dbContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                await dbContext.Database.EnsureCreatedAsync();

                dbContext.WorkflowInstances.Add(new()
                {
                    Id = "unassigned-row",
                    DefinitionId = "def",
                    DefinitionVersionId = "def-v1",
                    Version = 1,
                    CreatedAt = DateTimeOffset.UnixEpoch,
                    UpdatedAt = DateTimeOffset.UnixEpoch
                });

                dbContext.WorkflowInstances.Add(new()
                {
                    Id = "explicit-row",
                    DefinitionId = "def",
                    DefinitionVersionId = "def-v1",
                    Version = 1,
                    TenantId = "t1",
                    CreatedAt = DateTimeOffset.UnixEpoch,
                    UpdatedAt = DateTimeOffset.UnixEpoch
                });

                await dbContext.SaveChangesAsync();
            }

            await using (var verifyContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                var unassigned = await verifyContext.WorkflowInstances.IgnoreQueryFilters().SingleAsync(x => x.Id == "unassigned-row");
                var explicitRow = await verifyContext.WorkflowInstances.IgnoreQueryFilters().SingleAsync(x => x.Id == "explicit-row");

                Assert.Equal(Tenant.DefaultTenantId, unassigned.TenantId);
                Assert.Equal("t1", explicitRow.TenantId);
            }
        }
        finally
        {
            await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact(DisplayName = "With a tenant context an explicit foreign tenant id is rejected")]
    public async Task WritesWithTenantContext_RejectExplicitForeignTenant()
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-context-write-{Guid.NewGuid():N}.db");
        var tenantAccessor = new DefaultTenantAccessor();
        var services = BuildServices(databasePath, tenantAccessor);

        try
        {
            await using (var dbContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            using (tenantAccessor.PushContext(new Tenant { Id = "t1", Name = "t1" }))
            {
                await using var dbContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();

                dbContext.WorkflowInstances.Add(new()
                {
                    Id = "foreign-row",
                    DefinitionId = "def",
                    DefinitionVersionId = "def-v1",
                    Version = 1,
                    TenantId = "t2",
                    CreatedAt = DateTimeOffset.UnixEpoch,
                    UpdatedAt = DateTimeOffset.UnixEpoch
                });

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
                Assert.Contains("t2", exception.Message);
            }
        }
        finally
        {
            await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    private static ServiceProvider BuildServices(string databasePath, DefaultTenantAccessor tenantAccessor) =>
        new ServiceCollection()
            .AddLogging()
            .AddSingleton<ITenantAccessor>(tenantAccessor)
            .Configure<TenantsOptions>(options => options.IsEnabled = true)
            .AddScoped<IEntitySavingHandler, ApplyTenantId>()
            .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
            .AddSqliteEntityModelCreatingHandlers()
            .AddDbContextFactory<ManagementElsaDbContext>((_, builder) =>
                builder.UseElsaSqlite(typeof(RuntimeDbContextFactory).Assembly, $"Data Source={databasePath};Default Timeout=30"))
            .Decorate<IDbContextFactory<ManagementElsaDbContext>, TenantAwareDbContextFactory<ManagementElsaDbContext>>()
            .BuildServiceProvider();
}
