using Elsa.Persistence.VNext.Relational;
using Elsa.Persistence.VNext.Sqlite;
using Elsa.Secrets.Persistence.VNext;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.VNext.UnitTests;

public class SecretsSchemaPlanningTests
{
    private readonly SecretPersistenceSchemaProvider _schemaProvider = new();
    private readonly RelationalSchemaPlanner _planner = new(new SqliteTypeMapper());
    private readonly SqliteSchemaSqlRenderer _renderer = new();

    [Fact]
    public void SecretsSchema_DeclaresStorageIntentOnce()
    {
        var schema = _schemaProvider.DescribeSchema();
        var storageUnit = Assert.Single(schema.StorageUnits);

        Assert.Equal("Elsa.Secrets", schema.Name);
        Assert.Equal(1, schema.Version);
        Assert.Empty(schema.Tables);
        Assert.Equal("Secrets", storageUnit.Name);
        Assert.Equal("Elsa", storageUnit.Namespace);
        Assert.Equal(12, storageUnit.Fields.Count);
        Assert.Equal("PK_Secrets", storageUnit.Key?.Name);
        Assert.Equal(new[] { "Id" }, storageUnit.Key!.Columns);
        Assert.Equal(5, storageUnit.Indexes.Count);
        Assert.Contains(storageUnit.Indexes, x => x.Name == "IX_Secret_Name" && x.IsUnique && x.Columns.SequenceEqual(["Name"]));
    }

    [Fact]
    public void SqliteRenderer_ProducesProviderSpecificSchemaFromSecretsIntent()
    {
        var statements = RenderStatements();

        Assert.Contains(statements, sql => sql.Contains("CREATE TABLE IF NOT EXISTS \"Secrets\"", StringComparison.Ordinal));
        Assert.Contains(statements, sql => sql.Contains("\"CreatedAt\" TEXT NOT NULL", StringComparison.Ordinal));
        Assert.Contains(statements, sql => sql.Contains("CONSTRAINT \"PK_Secrets\" PRIMARY KEY (\"Id\")", StringComparison.Ordinal));
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Secret_Name\" ON \"Secrets\" (\"Name\");", statements);
    }

    [Fact]
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

        Assert.Equal("TEXT", columns["Id"].Type);
        Assert.False(columns["Id"].IsNullable);
        Assert.Equal("TEXT", columns["Versions"].Type);
        Assert.False(columns["Versions"].IsNullable);
        Assert.True(indexes["IX_Secret_Name"].IsUnique);
        Assert.Contains("IX_Secret_Status", indexes.Keys);
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
