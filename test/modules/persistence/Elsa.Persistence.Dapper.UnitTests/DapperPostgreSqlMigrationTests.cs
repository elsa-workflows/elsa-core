using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Services;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Runs the Dapper FluentMigrator assembly against a fresh PostgreSQL database.
/// Covers the FluentMigrator 7.2 IfDatabase("Postgres") vs "PostgreSQL" mismatch from issue #255.
/// </summary>
public sealed class DapperPostgreSqlMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("elsa")
        .WithUsername("elsa")
        .WithPassword("elsa")
        .Build();

    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact(DisplayName = "A fresh PostgreSQL DB applies every Dapper migration and can persist SerializedMetadata + AggregateFaultCount")]
    public async Task FreshPostgreSqlDatabase_MigratesAndPersistsActivityExecutionRecord()
    {
        // Arrange
        var store = CreateActivityExecutionStore();
        var record = new ActivityExecutionRecordRecord
        {
            Id = "rec-1",
            WorkflowInstanceId = "wf-1",
            ActivityId = "act-1",
            ActivityNodeId = "node-1",
            ActivityType = "Elsa.WriteLine",
            ActivityTypeVersion = 1,
            ActivityName = "WriteLine",
            StartedAt = DateTimeOffset.UtcNow,
            HasBookmarks = false,
            Status = "Finished",
            SerializedMetadata = """{"source":"fresh-db"}""",
            AggregateFaultCount = 2
        };

        // Act
        MigrateUp();
        await store.SaveAsync(record);
        var loaded = await store.FindAsync(q => q.Is(nameof(ActivityExecutionRecordRecord.Id), record.Id), cancellationToken: CancellationToken.None);

        // Assert
        var columns = ActivityExecutionRecordColumns();
        Assert.Contains(columns, name => name.Equals("AggregateFaultCount", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(columns, name => name.Equals("SerializedMetadata", StringComparison.OrdinalIgnoreCase));
        Assert.True(TableExists("Roles"));
        Assert.True(TableExists("WorkflowDefinitions"));
        Assert.True(TableExists("WorkflowInstances"));
        Assert.True(TableExists("Bookmarks"));
        Assert.True(TableExists("ActivityExecutionRecords"));
        Assert.True(TableExists("BookmarkQueueItems"));
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.AggregateFaultCount);
        Assert.Equal("""{"source":"fresh-db"}""", loaded.SerializedMetadata);
    }

    private Store<ActivityExecutionRecordRecord> CreateActivityExecutionStore()
    {
        var connectionProvider = new PostgreSqlDbConnectionProvider(_connectionString);
        return new Store<ActivityExecutionRecordRecord>(connectionProvider, new TestTenantAccessor(), "ActivityExecutionRecords");
    }

    private void MigrateUp()
    {
        using var services = CreateMigrator();
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    private ServiceProvider CreateMigrator() =>
        new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(typeof(Elsa.Persistence.Dapper.Migrations.Runtime.Initial).Assembly).For.Migrations())
            .BuildServiceProvider(false);

    private IReadOnlyCollection<string> ActivityExecutionRecordColumns()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<string>(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name ILIKE 'ActivityExecutionRecords'
            """).ToList();
    }

    private bool TableExists(string tableName)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.ExecuteScalar<long>(
            """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name ILIKE @tableName
            """,
            new { tableName }) == 1;
    }

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Tenant.DefaultTenantId;
        public Tenant? Tenant { get; private set; }

        public IDisposable PushContext(Tenant? tenant)
        {
            var previousTenant = Tenant;
            Tenant = tenant;
            return new Restore(() => Tenant = previousTenant);
        }

        private sealed class Restore(Action restore) : IDisposable
        {
            public void Dispose() => restore();
        }
    }
}
