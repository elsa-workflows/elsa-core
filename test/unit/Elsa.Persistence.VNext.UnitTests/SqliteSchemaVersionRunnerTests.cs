using Elsa.Persistence.VNext.Sqlite;
using Elsa.Secrets.Persistence.VNext;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class SqliteSchemaVersionRunnerTests
{
    private readonly SecretPersistenceSchemaProvider _schemaProvider = new();
    private readonly SqliteSchemaVersionRunner _runner = new();

    [Test]
    public async Task MigrateAsync_AppliesSchemaAndRecordsVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        var schema = _schemaProvider.DescribeSchema();

        var result = await _runner.MigrateAsync(connection, schema);
        var recordedVersion = await ReadScalarAsync<int>(connection, """SELECT MAX("Version") FROM "ElsaSchemaVersions" WHERE "SchemaName" = 'Elsa.Secrets';""");
        var tableCount = await ReadScalarAsync<int>(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Secrets';");

        await Assert.That(result.Applied).IsTrue();
        await Assert.That(result.FromVersion).IsEqualTo(0);
        await Assert.That(result.ToVersion).IsEqualTo(1);
        await Assert.That(result.Statements).IsNotEmpty();
        await Assert.That(recordedVersion).IsEqualTo(1);
        await Assert.That(tableCount).IsEqualTo(1);
    }

    [Test]
    public async Task MigrateAsync_DoesNotReapplyCurrentVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        var schema = _schemaProvider.DescribeSchema();

        var first = await _runner.MigrateAsync(connection, schema);
        var second = await _runner.MigrateAsync(connection, schema);
        var historyRows = await ReadScalarAsync<int>(connection, """SELECT COUNT(*) FROM "ElsaSchemaVersions" WHERE "SchemaName" = 'Elsa.Secrets';""");

        await Assert.That(first.Applied).IsTrue();
        await Assert.That(second.Applied).IsFalse();
        await Assert.That(second.FromVersion).IsEqualTo(1);
        await Assert.That(second.ToVersion).IsEqualTo(1);
        await Assert.That(second.Statements).IsEmpty();
        await Assert.That(historyRows).IsEqualTo(1);
    }

    private static async Task<T> ReadScalarAsync<T>(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }
}
