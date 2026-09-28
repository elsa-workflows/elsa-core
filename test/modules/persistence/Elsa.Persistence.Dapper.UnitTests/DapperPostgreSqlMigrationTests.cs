using Dapper;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Runs the Dapper FluentMigrator assembly against a fresh PostgreSQL database.
/// Covers the FluentMigrator 7.2 IfDatabase("Postgres") vs PostgreSQL* mismatch from issue #255.
/// Persist is not asserted here: the Dapper PostgreSQL dialect emits unquoted identifiers
/// while FluentMigrator force-quotes table/column names, and PostgreSqlDialect.Upsert
/// omits the primary key from the insert list.
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

    [Fact(DisplayName = "A fresh PostgreSQL DB applies every Dapper migration and creates the expected tables")]
    public void FreshPostgreSqlDatabase_MigratesAndCreatesExpectedTables()
    {
        MigrateUp();

        var columns = ActivityExecutionRecordColumns();
        Assert.Contains(columns, name => name.Equals("AggregateFaultCount", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(columns, name => name.Equals("SerializedMetadata", StringComparison.OrdinalIgnoreCase));
        Assert.True(TableExists("Roles"));
        Assert.True(TableExists("WorkflowDefinitions"));
        Assert.True(TableExists("WorkflowInstances"));
        Assert.True(TableExists("Bookmarks"));
        Assert.True(TableExists("ActivityExecutionRecords"));
        Assert.True(TableExists("BookmarkQueueItems"));
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
}
