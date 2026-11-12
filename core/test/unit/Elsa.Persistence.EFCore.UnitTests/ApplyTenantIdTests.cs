using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// Tests the tenant stamping rule of <see cref="ApplyTenantId"/>, mirroring the Dapper
/// <c>Store&lt;T&gt;.SetTenantId</c> rule from #9017 (issue #9022).
/// </summary>
public class ApplyTenantIdTests
{
    [Theory(DisplayName = "Named ambient tenant stamps null/empty tenant ids and keeps '*', matching and explicit ids")]
    [InlineData(null, "t1")]
    [InlineData("", "t1")]
    [InlineData("*", "*")]
    [InlineData("t1", "t1")]
    public async Task NamedAmbientTenant_StampsMissingAndKeepsExplicit(string? tenantId, string expectedTenantId)
    {
        var (exception, storedTenantId) = await SaveWithAmbientAsync("t1", tenantId);

        Assert.Null(exception);
        Assert.Equal(expectedTenantId, storedTenantId);
    }

    [Fact]
    public async Task NamedAmbientTenant_WhenExplicitTenantDiffers_ThrowsAndSavesNothing()
    {
        var (exception, storedTenantId) = await SaveWithAmbientAsync("t1", "t2");

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Null(storedTenantId);
    }

    [Theory(DisplayName = "A null ambient tenant keeps explicit tenant ids and stamps missing ids with the default tenant")]
    [InlineData("t1", "t1")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("*", "*")]
    public async Task NullAmbientContext_KeepsExplicitTenantAndStampsMissingWithDefault(string? tenantId, string? expectedTenantId)
    {
        var (exception, storedTenantId) = await SaveWithAmbientAsync(null, tenantId);

        Assert.Null(exception);
        Assert.Equal(expectedTenantId, storedTenantId);
    }

    [Theory(DisplayName = "The default tenant context stamps unassigned entities with the default tenant id")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("*", "*")]
    public async Task DefaultTenantContext_StampsMissingWithDefaultTenant(string? tenantId, string? expectedTenantId)
    {
        var (exception, storedTenantId) = await SaveWithAmbientAsync(Tenant.DefaultTenantId, tenantId);

        Assert.Null(exception);
        Assert.Equal(expectedTenantId, storedTenantId);
    }

    [Fact]
    public async Task DefaultTenantContext_WhenExplicitTenantDiffers_ThrowsAndSavesNothing()
    {
        var (exception, storedTenantId) = await SaveWithAmbientAsync(Tenant.DefaultTenantId, "t1");

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Null(storedTenantId);
    }

    [Theory(DisplayName = "When multitenancy is disabled the entity tenant id is left untouched")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("*")]
    [InlineData("t2")]
    public async Task MultitenancyDisabled_LeavesEntityUntouched(string? tenantId)
    {
        var (exception, storedTenantId) = await SaveWithAmbientAsync("t1", tenantId, multitenancyEnabled: false);

        Assert.Null(exception);
        Assert.Equal(tenantId, storedTenantId);
    }

    [Theory(DisplayName = "The db context factory normalizes TenantId to empty string and tracks whether a tenant context exists")]
    [InlineData(false, "", false)]
    [InlineData(true, "", true)]
    public async Task DbContextFactory_NormalizesTenantIdAndTracksContextPresence(bool pushDefaultTenant, string expectedTenantId, bool expectedHasTenantContext)
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-apply-tenant-id-{Guid.NewGuid():N}.db");
        var tenantAccessor = new DefaultTenantAccessor();
        await using var services = BuildServices(databasePath, tenantAccessor);

        try
        {
            using var tenantContext = pushDefaultTenant ? tenantAccessor.PushContext(Tenant.Default) : null;
            await using var dbContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();

            Assert.Equal(expectedTenantId, dbContext.TenantId);
            Assert.Equal(expectedHasTenantContext, dbContext.HasTenantContext);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    private static async Task<(Exception? Exception, string? StoredTenantId)> SaveWithAmbientAsync(
        string? ambientTenantId,
        string? entityTenantId,
        bool multitenancyEnabled = true)
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-apply-tenant-id-{Guid.NewGuid():N}.db");
        var tenantAccessor = new DefaultTenantAccessor();
        var services = BuildServices(databasePath, tenantAccessor, multitenancyEnabled);

        try
        {
            await using (var setupContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                await setupContext.Database.EnsureCreatedAsync();
            }

            Exception? exception;
            await using (var dbContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                // The ambient tenant is assigned the same way TenantAwareDbContextFactory assigns it:
                // TenantId is always normalized; HasTenantContext distinguishes a real context from its absence.
                dbContext.TenantId = (ambientTenantId).NormalizeTenantId();
                dbContext.HasTenantContext = ambientTenantId is not null;

                dbContext.WorkflowInstances.Add(new()
                {
                    Id = "instance-1",
                    DefinitionId = "def-1",
                    DefinitionVersionId = "def-1-v1",
                    Version = 1,
                    TenantId = entityTenantId,
                    CreatedAt = DateTimeOffset.UnixEpoch,
                    UpdatedAt = DateTimeOffset.UnixEpoch
                });

                try
                {
                    await dbContext.SaveChangesAsync();
                    exception = null;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            await using (var verifyContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                var stored = await verifyContext.WorkflowInstances.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == "instance-1");
                return (exception, stored?.TenantId);
            }
        }
        finally
        {
            await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    private static ServiceProvider BuildServices(
        string databasePath,
        DefaultTenantAccessor tenantAccessor,
        bool multitenancyEnabled = true) =>
        new ServiceCollection()
            .AddLogging()
            .AddSingleton<ITenantAccessor>(tenantAccessor)
            .Configure<TenantsOptions>(options => options.IsEnabled = multitenancyEnabled)
            .AddScoped<IEntitySavingHandler, ApplyTenantId>()
            .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
            .AddSqliteEntityModelCreatingHandlers()
            .AddDbContextFactory<ManagementElsaDbContext>((_, builder) => builder.UseElsaSqlite(typeof(RuntimeDbContextFactory).Assembly, $"Data Source={databasePath};Default Timeout=30"))
            .Decorate<IDbContextFactory<ManagementElsaDbContext>, TenantAwareDbContextFactory<ManagementElsaDbContext>>()
            .BuildServiceProvider();
}
