using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Services;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Runs the Dapper FluentMigrator assembly against a fresh SQLite file.
/// Covers the V3_5 / V3_7 AggregateFaultCount overlap from issue #250.
/// </summary>
public sealed class DapperRuntimeMigrationTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-runtime-migrations-{Guid.NewGuid():N}.db");
    private readonly string _connectionString;

    public DapperRuntimeMigrationTests()
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
    }

    [Fact(DisplayName = "A fresh SQLite DB applies every runtime migration and can persist SerializedMetadata + AggregateFaultCount")]
    public async Task FreshSqliteDatabase_MigratesAndPersistsActivityExecutionRecord()
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
        Assert.Contains("AggregateFaultCount", columns);
        Assert.Contains("SerializedMetadata", columns);
        Assert.True(TableExists("Roles"));
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.AggregateFaultCount);
        Assert.Equal("""{"source":"fresh-db"}""", loaded.SerializedMetadata);
    }

    [Fact(DisplayName = "A DB stopped at V3_5 (20006) upgrades to latest without a duplicate-column error")]
    public void UpgradeFromV3_5_AppliesV3_7WithoutDuplicateAggregateFaultCount()
    {
        // Arrange
        MigrateUp(20006);
        var columnsAfterV35 = ActivityExecutionRecordColumns();
        Assert.Contains("AggregateFaultCount", columnsAfterV35);
        Assert.DoesNotContain("SerializedMetadata", columnsAfterV35);

        // Act
        MigrateUp();

        // Assert
        var columns = ActivityExecutionRecordColumns();
        Assert.Contains("AggregateFaultCount", columns);
        Assert.Contains("SerializedMetadata", columns);
        Assert.Contains("SchedulingActivityExecutionId", columns);
        Assert.Contains("CallStackDepth", columns);
        Assert.True(TableExists("Roles"));
    }

    [Fact(DisplayName = "Replaying 10001/20001 when tables already exist does not fail (no VersionInfo rows)")]
    public void ReplayingCreateTableMigrations_WhenTablesAlreadyExist_DoesNotFail()
    {
        MigrateUp();
        using (var connection = new SqliteConnection(_connectionString))
            connection.Execute("DELETE FROM VersionInfo WHERE Version IN (10001, 20001)");

        MigrateUp();

        Assert.True(TableExists("WorkflowDefinitions"));
        Assert.True(TableExists("WorkflowInstances"));
        Assert.True(TableExists("Bookmarks"));
        Assert.True(TableExists("ActivityExecutionRecords"));
        Assert.True(TableExists("Roles"));
    }

    [Fact(DisplayName = "Rolling back V3_7 leaves V3_5's AggregateFaultCount so V3_5.Down() does not double-drop")]
    public void RollingBackThroughV3_7AndV3_5_DoesNotDoubleDropAggregateFaultCount()
    {
        // Arrange
        MigrateUp();

        // Act
        MigrateDown(20006);

        // Assert: V3_7 rolled back, V3_5 still owns the column
        var afterV37Down = ActivityExecutionRecordColumns();
        Assert.Contains("AggregateFaultCount", afterV37Down);
        Assert.DoesNotContain("SerializedMetadata", afterV37Down);

        // Act: V3_5.Down() must be able to drop its own column
        MigrateDown(20005);

        // Assert
        Assert.DoesNotContain("AggregateFaultCount", ActivityExecutionRecordColumns());
    }

    public void Dispose()
    {
        File.Delete(_databasePath);
    }

    private Store<ActivityExecutionRecordRecord> CreateActivityExecutionStore()
    {
        var connectionProvider = new SqliteDbConnectionProvider(_connectionString);
        return new Store<ActivityExecutionRecordRecord>(connectionProvider, new TestTenantAccessor(), "ActivityExecutionRecords");
    }

    private void MigrateUp(long? targetVersion = null)
    {
        using var services = CreateMigrator();
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        if (targetVersion is { } version)
            runner.MigrateUp(version);
        else
            runner.MigrateUp();
    }

    private void MigrateDown(long targetVersion)
    {
        using var services = CreateMigrator();
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateDown(targetVersion);
    }

    private ServiceProvider CreateMigrator() =>
        new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(typeof(Elsa.Persistence.Dapper.Migrations.Runtime.Initial).Assembly).For.Migrations())
            .BuildServiceProvider(false);

    private IReadOnlyCollection<string> ActivityExecutionRecordColumns()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<string>("SELECT name FROM pragma_table_info('ActivityExecutionRecords')").ToList();
    }

    private bool TableExists(string tableName)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName",
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
