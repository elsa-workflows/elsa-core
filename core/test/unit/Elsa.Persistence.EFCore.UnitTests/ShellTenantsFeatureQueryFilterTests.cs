using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Management;
using Elsa.Tenants.ShellFeatures;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// Verifies tenancy in a shell host using only the real shell features: no tenant handlers are registered by hand.
/// </summary>
public class ShellTenantsFeatureQueryFilterTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-shell-tenants-{Guid.NewGuid():N}.db");
    private readonly TestTenantAccessor _tenantAccessor = new();
    private ServiceProvider _serviceProvider = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ITenantAccessor>(_tenantAccessor);
        new TenantsFeature().ConfigureServices(services);
        new SqliteWorkflowInstancePersistenceShellFeature { ConnectionString = $"Data Source={_databasePath};Default Timeout=30" }.ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }

    [Fact(DisplayName = "A tenant sees its own rows and tenant-agnostic rows only")]
    public async Task Tenant_SeesOwnAndAgnosticRowsOnly()
    {
        // Act
        var visibleIds = await ListVisibleIdsAsync("t1");

        // Assert
        Assert.Equal(["i-agnostic", "i-t1"], visibleIds);
    }

    [Fact(DisplayName = "The default tenant sees default, unassigned and tenant-agnostic rows only")]
    public async Task DefaultTenant_SeesDefaultUnassignedAndAgnosticRowsOnly()
    {
        // Act
        var visibleIds = await ListVisibleIdsAsync(Tenant.DefaultTenantId);

        // Assert
        Assert.Equal(["i-agnostic", "i-default", "i-null"], visibleIds);
    }

    [Fact(DisplayName = "A row saved without a tenant ID is stamped with the current tenant")]
    public async Task NewRow_IsStampedWithCurrentTenant()
    {
        // Arrange
        using var _ = UseTenant("t1");
        using var scope = _serviceProvider.CreateScope();
        await using var dbContext = await CreateDbContextAsync(scope);

        // Act
        dbContext.WorkflowInstances.Add(CreateInstance("i-new", tenantId: null));
        await dbContext.SaveChangesAsync();

        // Assert
        var stored = await dbContext.WorkflowInstances.AsNoTracking().IgnoreQueryFilters().SingleAsync(x => x.Id == "i-new");
        Assert.Equal("t1", stored.TenantId);
    }

    private async Task SeedAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        await using var dbContext = await CreateDbContextAsync(scope);
        await dbContext.Database.EnsureCreatedAsync();

        // Explicit tenant IDs are kept as they are, so every case below is stored exactly as written.
        dbContext.WorkflowInstances.AddRange(
            CreateInstance("i-t1", "t1"),
            CreateInstance("i-t2", "t2"),
            CreateInstance("i-agnostic", Tenant.AgnosticTenantId),
            CreateInstance("i-default", Tenant.DefaultTenantId),
            CreateInstance("i-null", "placeholder"));
        await dbContext.SaveChangesAsync();

        // Rows written before tenancy was enabled have no tenant ID.
        await dbContext.WorkflowInstances
            .IgnoreQueryFilters()
            .Where(x => x.Id == "i-null")
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.TenantId, (string?)null));
    }

    private async Task<List<string>> ListVisibleIdsAsync(string tenantId)
    {
        using var _ = UseTenant(tenantId);
        using var scope = _serviceProvider.CreateScope();
        await using var dbContext = await CreateDbContextAsync(scope);

        return await dbContext.WorkflowInstances.Select(x => x.Id).OrderBy(x => x).ToListAsync();
    }

    private IDisposable UseTenant(string tenantId) =>
        _tenantAccessor.PushContext(tenantId == Tenant.DefaultTenantId ? Tenant.Default : new Tenant { Id = tenantId, Name = tenantId });

    private static Task<ManagementElsaDbContext> CreateDbContextAsync(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();

    private static WorkflowInstance CreateInstance(string id, string? tenantId) => new()
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
