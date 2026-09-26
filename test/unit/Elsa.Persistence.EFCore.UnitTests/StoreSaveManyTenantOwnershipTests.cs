using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared.Multitenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Elsa.Persistence.EFCore.UnitTests;

public abstract class StoreSaveManyTenantOwnershipTests
{
    protected abstract Task<OwnershipStoreScenario> CreateScenarioAsync(string tenantId, bool tenantsEnabled = true);

    [Fact]
    public async Task SaveManyAsync_WhenOtherTenantOwnsId_ThrowsAndLeavesOwnerAndPayload()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("shared", "tenant-a", "original")], x => x.Id, onSaving: null);

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Store.SaveManyAsync([Row("shared", "tenant-b", "stolen")], x => x.Id, onSaving: null));
        }

        var remaining = await scenario.FindAsync("shared");
        AssertUnchanged(remaining, "tenant-a", "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenMixedBatchContainsForeignId_WritesNothing()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("owned", "tenant-a", "original")], x => x.Id, onSaving: null);

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.Store.SaveManyAsync(
            [
                Row("new-from-b", "tenant-b", "should-not-land"),
                Row("owned", "tenant-b", "stolen")
            ], x => x.Id, onSaving: null));
        }

        Assert.Null(await scenario.FindAsync("new-from-b"));
        AssertUnchanged(await scenario.FindAsync("owned"), "tenant-a", "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenSameTenantOwnsId_UpdatesPayload()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("row-a", "tenant-a", "before")], x => x.Id, onSaving: null);

        await scenario.Store.SaveManyAsync([Row("row-a", "tenant-a", "after")], x => x.Id, onSaving: null);

        AssertUnchanged(await scenario.FindAsync("row-a"), "tenant-a", "after");
    }

    [Fact]
    public async Task SaveManyAsync_WhenPopulatorResavesStarUnderNamedTenant_KeepsStar()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("shared", Tenant.AgnosticTenantId, "before")], x => x.Id, onSaving: null);

        await scenario.Store.SaveManyAsync([Row("shared", Tenant.AgnosticTenantId, "after")], x => x.Id, onSaving: null);

        AssertUnchanged(await scenario.FindAsync("shared"), Tenant.AgnosticTenantId, "after");
    }

    [Fact]
    public async Task SaveManyAsync_WhenNamedTenantSavesNullTenantIdOverStar_ThrowsAndLeavesStar()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("shared", Tenant.AgnosticTenantId, "original")], x => x.Id, onSaving: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scenario.Store.SaveManyAsync([Row("shared", tenantId: null, payload: "stolen")], x => x.Id, onSaving: null));

        AssertUnchanged(await scenario.FindAsync("shared"), Tenant.AgnosticTenantId, "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenDefaultTenantUpdatesLegacyNullRow_Succeeds()
    {
        await using var scenario = await CreateScenarioAsync(Tenant.DefaultTenantId);
        await scenario.Store.SaveManyAsync([Row("legacy", Tenant.DefaultTenantId, "before")], x => x.Id, onSaving: null);
        await scenario.ClearTenantIdAsync("legacy");

        await scenario.Store.SaveManyAsync([Row("legacy", Tenant.DefaultTenantId, "after")], x => x.Id, onSaving: null);

        var found = await scenario.FindAsync("legacy");
        Assert.NotNull(found);
        Assert.Equal("after", found.Payload);
        Assert.True(string.IsNullOrEmpty(found.TenantId));
    }

    [Fact]
    public async Task SaveManyAsync_WhenTenancyIsDisabled_OverwritesForeignId()
    {
        await using var scenario = await CreateScenarioAsync("tenant-b", tenantsEnabled: false);
        await scenario.Store.SaveManyAsync([Row("shared", "tenant-a", "original")], x => x.Id, onSaving: null);

        await scenario.Store.SaveManyAsync([Row("shared", "tenant-b", "taken")], x => x.Id, onSaving: null);

        AssertUnchanged(await scenario.FindAsync("shared"), "tenant-b", "taken");
    }

    private static void AssertUnchanged(OwnedRow? row, string tenantId, string payload)
    {
        Assert.NotNull(row);
        Assert.Equal(tenantId, row.TenantId);
        Assert.Equal(payload, row.Payload);
    }

    private static OwnedRow Row(string id, string? tenantId, string payload) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Payload = payload
        };
}

public sealed class SqliteStoreSaveManyTenantOwnershipTests : StoreSaveManyTenantOwnershipTests
{
    protected override Task<OwnershipStoreScenario> CreateScenarioAsync(string tenantId, bool tenantsEnabled = true) =>
        OwnershipStoreScenario.CreateSqliteAsync(tenantId, tenantsEnabled);
}

[Collection(PostgreSqlStoreSaveManyCollection.Name)]
public sealed class PostgreSqlStoreSaveManyTenantOwnershipTests : StoreSaveManyTenantOwnershipTests
{
    private readonly PostgreSqlStoreSaveManyFixture _fixture;

    public PostgreSqlStoreSaveManyTenantOwnershipTests(PostgreSqlStoreSaveManyFixture fixture)
    {
        _fixture = fixture;
    }

    protected override Task<OwnershipStoreScenario> CreateScenarioAsync(string tenantId, bool tenantsEnabled = true)
    {
        Assert.True(_fixture.IsAvailable, _fixture.SkipReason);
        return OwnershipStoreScenario.CreatePostgreSqlAsync(_fixture.ConnectionString, tenantId, tenantsEnabled);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlStoreSaveManyCollection : ICollectionFixture<PostgreSqlStoreSaveManyFixture>
{
    public const string Name = "StoreSaveMany:PostgreSql";
}

public sealed class PostgreSqlStoreSaveManyFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public bool IsAvailable { get; private set; }
    public string ConnectionString { get; private set; } = "";
    public string SkipReason { get; private set; } = "PostgreSQL is not available.";

    public async Task InitializeAsync()
    {
        var fromEnv = Environment.GetEnvironmentVariable("ELSA_TEST_POSTGRES");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            ConnectionString = fromEnv;
            IsAvailable = true;
            return;
        }

        try
        {
            _container = new PostgreSqlBuilder().Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            IsAvailable = true;
            return;
        }
        catch (Exception containerException)
        {
            var local = "Host=127.0.0.1;Port=5432;Username=postgres;Password=postgres;Database=postgres";
            try
            {
                await using var connection = new Npgsql.NpgsqlConnection(local);
                await connection.OpenAsync();
                ConnectionString = local;
                IsAvailable = true;
                return;
            }
            catch (Exception localException)
            {
                SkipReason =
                    $"PostgreSQL is unavailable. Testcontainers: {containerException.Message} Local: {localException.Message}";
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}

public sealed class OwnershipStoreScenario : IAsyncDisposable
{
    private readonly Func<ValueTask> _disposeAsync;
    private readonly IDbContextFactory<OwnershipDbContext> _dbContextFactory;

    private OwnershipStoreScenario(
        TestTenantAccessor tenantAccessor,
        Store<OwnershipDbContext, OwnedRow> store,
        IDbContextFactory<OwnershipDbContext> dbContextFactory,
        Func<ValueTask> disposeAsync)
    {
        TenantAccessor = tenantAccessor;
        Store = store;
        _dbContextFactory = dbContextFactory;
        _disposeAsync = disposeAsync;
    }

    public TestTenantAccessor TenantAccessor { get; }
    public Store<OwnershipDbContext, OwnedRow> Store { get; }

    public IDisposable UseTenant(string tenantId) =>
        TenantAccessor.PushContext(tenantId == Tenant.DefaultTenantId
            ? Tenant.Default
            : new Tenant { Id = tenantId, Name = tenantId });

    public async Task<OwnedRow?> FindAsync(string id)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Rows.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task ClearTenantIdAsync(string id)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.Rows
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.TenantId, (string?)null));
    }

    public ValueTask DisposeAsync() => _disposeAsync();

    public static async Task<OwnershipStoreScenario> CreateSqliteAsync(string tenantId, bool tenantsEnabled)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return await CreateAsync(
            tenantId,
            tenantsEnabled,
            builder => builder.UseSqlite(connection),
            async () =>
            {
                await connection.DisposeAsync();
            });
    }

    public static async Task<OwnershipStoreScenario> CreatePostgreSqlAsync(string connectionString, string tenantId, bool tenantsEnabled)
    {
        var isolatedConnectionString = await CreateIsolatedDatabaseAsync(connectionString);
        return await CreateAsync(
            tenantId,
            tenantsEnabled,
            builder => builder.UseNpgsql(isolatedConnectionString),
            () => ValueTask.CompletedTask);
    }

    private static async Task<OwnershipStoreScenario> CreateAsync(
        string tenantId,
        bool tenantsEnabled,
        Action<DbContextOptionsBuilder> configure,
        Func<ValueTask> disposeAsync)
    {
        var tenantAccessor = new TestTenantAccessor(tenantId);
        var services = new ServiceCollection()
            .AddSingleton<ITenantAccessor>(tenantAccessor)
            .Configure<TenantsOptions>(options => options.IsEnabled = tenantsEnabled)
            .AddDbContextFactory<OwnershipDbContext>((_, builder) => configure(builder))
            .AddSingleton(sp => new Store<OwnershipDbContext, OwnedRow>(
                sp.GetRequiredService<IDbContextFactory<OwnershipDbContext>>(),
                sp))
            .BuildServiceProvider();

        var factory = services.GetRequiredService<IDbContextFactory<OwnershipDbContext>>();
        await using (var dbContext = await factory.CreateDbContextAsync())
            await dbContext.Database.EnsureCreatedAsync();

        return new OwnershipStoreScenario(
            tenantAccessor,
            services.GetRequiredService<Store<OwnershipDbContext, OwnedRow>>(),
            factory,
            async () =>
            {
                await services.DisposeAsync();
                await disposeAsync();
            });
    }

    private static async Task<string> CreateIsolatedDatabaseAsync(string connectionString)
    {
        var database = $"elsa_savemany_{Guid.NewGuid():N}";
        var admin = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        };

        await using (var connection = new Npgsql.NpgsqlConnection(admin.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"""CREATE DATABASE "{database}" """;
            await command.ExecuteNonQueryAsync();
        }

        var target = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = database
        };
        return target.ConnectionString;
    }
}

public sealed class OwnershipDbContext(DbContextOptions<OwnershipDbContext> options) : DbContext(options)
{
    public DbSet<OwnedRow> Rows => Set<OwnedRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Elsa");
        modelBuilder.Entity<OwnedRow>().ToTable("OwnedRows", "Elsa");
        modelBuilder.Entity<OwnedRow>().HasKey(x => x.Id);
        modelBuilder.Entity<OwnedRow>().Property(x => x.Payload).IsRequired();
    }
}

public sealed class OwnedRow : Entity
{
    public string Payload { get; set; } = "";
}
