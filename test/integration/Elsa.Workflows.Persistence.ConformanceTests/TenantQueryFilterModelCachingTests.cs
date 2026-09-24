using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Tenants.Options;
using Elsa.Workflows.Management.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Persistence.ConformanceTests;

public class TenantQueryFilterModelCachingTests
{
    [Fact]
    public async Task DisabledModelCreatedFirstDoesNotDisableTenantFilteringForLaterContexts()
    {
        await using var fixture = await Fixture.CreateAsync(initiallyEnabled: false);

        // Build the context model while multitenancy is disabled.
        Assert.Equal(5, (await fixture.QueryIdsAsync("tenant-a")).Count);
        fixture.TenantsOptions.IsEnabled = true;

        Assert.Equal(["tenant-agnostic", "tenant-b"], await fixture.QueryIdsAsync("tenant-b"));
        Assert.Equal(["tenant-a", "tenant-agnostic"], await fixture.QueryIdsAsync("tenant-a"));
        Assert.Equal(["default-empty", "default-null", "tenant-agnostic"], await fixture.QueryIdsAsync(string.Empty));
    }

    [Fact]
    public async Task EnabledModelCreatedFirstDoesNotFilterLaterContextsWhenMultitenancyIsDisabled()
    {
        await using var fixture = await Fixture.CreateAsync(initiallyEnabled: true);

        // Build the context model while multitenancy is enabled.
        Assert.Equal(["tenant-a", "tenant-agnostic"], await fixture.QueryIdsAsync("tenant-a"));
        fixture.TenantsOptions.IsEnabled = false;

        Assert.Equal(5, (await fixture.QueryIdsAsync("tenant-b")).Count);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IDbContextFactory<ManagementElsaDbContext> _contextFactory;
        private readonly SqliteConnection _connection;

        private Fixture(
            ServiceProvider serviceProvider,
            IDbContextFactory<ManagementElsaDbContext> contextFactory,
            SqliteConnection connection,
            TenantsOptions tenantsOptions)
        {
            _serviceProvider = serviceProvider;
            _contextFactory = contextFactory;
            _connection = connection;
            TenantsOptions = tenantsOptions;
        }

        public TenantsOptions TenantsOptions { get; }

        public static async Task<Fixture> CreateAsync(bool initiallyEnabled)
        {
            var tenantsOptions = new TenantsOptions { IsEnabled = initiallyEnabled };
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IOptions<TenantsOptions>>(Microsoft.Extensions.Options.Options.Create(tenantsOptions))
                .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
                .AddSqliteEntityModelCreatingHandlers()
                .AddDbContextFactory<ManagementElsaDbContext>(builder => builder.UseSqlite(connection));
            var serviceProvider = services.BuildServiceProvider();
            var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>();
            var fixture = new Fixture(serviceProvider, contextFactory, connection, tenantsOptions);

            await using var dbContext = await contextFactory.CreateDbContextAsync();
            await dbContext.Database.EnsureCreatedAsync();
            dbContext.WorkflowInstances.AddRange(
                CreateWorkflowInstance("tenant-a", "tenant-a"),
                CreateWorkflowInstance("tenant-b", "tenant-b"),
                CreateWorkflowInstance("default-null", null),
                CreateWorkflowInstance("default-empty", string.Empty),
                CreateWorkflowInstance("tenant-agnostic", "*"));
            await dbContext.SaveChangesAsync();

            return fixture;
        }

        public async Task<IReadOnlyList<string>> QueryIdsAsync(string tenantId)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();
            dbContext.TenantId = tenantId;
            return await dbContext.WorkflowInstances.OrderBy(x => x.Id).Select(x => x.Id).ToListAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private static WorkflowInstance CreateWorkflowInstance(string id, string? tenantId) => new()
        {
            Id = id,
            TenantId = tenantId,
            DefinitionId = "definition",
            DefinitionVersionId = "definition-version",
            Status = Elsa.Workflows.WorkflowStatus.Running,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
