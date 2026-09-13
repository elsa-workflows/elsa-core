using Elsa.Persistence.VNext.Relational;
using Elsa.Persistence.VNext.Sqlite;
using Elsa.Secrets.Persistence.VNext;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class SecretsSchemaPlanningTests
{
    private readonly SecretPersistenceSchemaProvider _schemaProvider = new();
    private readonly RelationalSchemaPlanner _planner = new(new SqliteTypeMapper());
    private readonly SqliteSchemaSqlRenderer _renderer = new();

    [Test]
    public async Task SecretsSchema_DeclaresStorageIntentOnce()
    {
        var schema = _schemaProvider.DescribeSchema();
        var storageUnit = await Assert.That(schema.StorageUnits).HasSingleItem();

        await Assert.That(schema.Name).IsEqualTo("Elsa.Secrets");
        await Assert.That(schema.Version).IsEqualTo(1);
        await Assert.That(schema.Tables).IsEmpty();
        await Assert.That(storageUnit.Name).IsEqualTo("Secrets");
        await Assert.That(storageUnit.Namespace).IsEqualTo("Elsa");
        await Assert.That(storageUnit.Fields.Count).IsEqualTo(12);
        await Assert.That(storageUnit.Key?.Name).IsEqualTo("PK_Secrets");
        await Assert.That(storageUnit.Key!.Columns).IsEquivalentTo(new[] { "Id" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(storageUnit.Indexes.Count).IsEqualTo(5);
        await Assert.That(storageUnit.Indexes).Contains(x => x.Name == "IX_Secret_Name" && x.IsUnique && x.Columns.SequenceEqual(["Name"]));
    }

    [Test]
    public async Task SqliteRenderer_ProducesProviderSpecificSchemaFromSecretsIntent()
    {
        var statements = RenderStatements();

        await Assert.That(statements).Contains(sql => sql.Contains("CREATE TABLE IF NOT EXISTS \"Secrets\"", StringComparison.Ordinal));
        await Assert.That(statements).Contains(sql => sql.Contains("\"CreatedAt\" TEXT NOT NULL", StringComparison.Ordinal));
        await Assert.That(statements).Contains(sql => sql.Contains("CONSTRAINT \"PK_Secrets\" PRIMARY KEY (\"Id\")", StringComparison.Ordinal));
        await Assert.That(statements).Contains("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Secret_Name\" ON \"Secrets\" (\"Name\");");
    }

    [Test]
    public async Task SqliteRenderer_CreatesExecutableSchema()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        foreach (var statement in RenderStatements())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = statement;
            await command.ExecuteNonQueryAsync();
        }

        var columns = await ReadColumnsAsync(connection, "Secrets");
        var indexes = await ReadIndexesAsync(connection, "Secrets");

        await Assert.That(columns["Id"].Type).IsEqualTo("TEXT");
        await Assert.That(columns["Id"].IsNullable).IsFalse();
        await Assert.That(columns["Versions"].Type).IsEqualTo("TEXT");
        await Assert.That(columns["Versions"].IsNullable).IsFalse();
        await Assert.That(indexes["IX_Secret_Name"].IsUnique).IsTrue();
        await Assert.That(indexes.Keys).Contains("IX_Secret_Status");
    }

    private IReadOnlyList<string> RenderStatements()
    {
        var schema = _schemaProvider.DescribeSchema();
        var plan = _planner.Plan(schema);
        return _renderer.Render(plan);
    }

    private static async Task<Dictionary<string, SqliteColumnInfo>> ReadColumnsAsync(SqliteConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteLiteral(table)})";

        var columns = new Dictionary<string, SqliteColumnInfo>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(1);
            var type = reader.GetString(2);
            var notNull = reader.GetInt32(3) == 1;
            columns[name] = new SqliteColumnInfo(type, !notNull);
        }

        return columns;
    }

    private static async Task<Dictionary<string, SqliteIndexInfo>> ReadIndexesAsync(SqliteConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA index_list({QuoteLiteral(table)})";

        var indexes = new Dictionary<string, SqliteIndexInfo>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(1);
            var unique = reader.GetInt32(2) == 1;
            indexes[name] = new SqliteIndexInfo(unique);
        }

        return indexes;
    }

    private static string QuoteLiteral(string value)
    {
        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
    }

    private record SqliteColumnInfo(string Type, bool IsNullable);

    private record SqliteIndexInfo(bool IsUnique);
}
