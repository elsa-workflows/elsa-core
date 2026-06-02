using Elsa.Persistence.VNext.Sqlite;
using Elsa.Secrets.Persistence.VNext;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.VNext.UnitTests;

public class SqliteSchemaVersionRunnerTests
{
    private readonly SecretPersistenceSchemaProvider _schemaProvider = new();
    private readonly SqliteSchemaVersionRunner _runner = new();

    [Fact]
    public async Task MigrateAsync_AppliesSchemaAndRecordsVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        var schema = _schemaProvider.DescribeSchema();

        var result = await _runner.MigrateAsync(connection, schema);
        var recordedVersion = await ReadScalarAsync<int>(connection, """SELECT MAX("Version") FROM "ElsaSchemaVersions" WHERE "SchemaName" = 'Elsa.Secrets';""");
        var tableCount = await ReadScalarAsync<int>(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Secrets';");

        Assert.True(result.Applied);
        Assert.Equal(0, result.FromVersion);
        Assert.Equal(1, result.ToVersion);
        Assert.NotEmpty(result.Statements);
        Assert.Equal(1, recordedVersion);
        Assert.Equal(1, tableCount);
    }

    [Fact]
    public async Task MigrateAsync_DoesNotReapplyCurrentVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        var schema = _schemaProvider.DescribeSchema();

        var first = await _runner.MigrateAsync(connection, schema);
        var second = await _runner.MigrateAsync(connection, schema);
        var historyRows = await ReadScalarAsync<int>(connection, """SELECT COUNT(*) FROM "ElsaSchemaVersions" WHERE "SchemaName" = 'Elsa.Secrets';""");

        Assert.True(first.Applied);
        Assert.False(second.Applied);
        Assert.Equal(1, second.FromVersion);
        Assert.Equal(1, second.ToVersion);
        Assert.Empty(second.Statements);
        Assert.Equal(1, historyRows);
    }

    private static async Task<T> ReadScalarAsync<T>(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }
}
