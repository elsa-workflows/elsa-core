using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Sqlite.Options;
using Elsa.ModularPersistence.Sqlite.Services;
using Microsoft.Data.Sqlite;

namespace Elsa.ModularPersistence.Relational.IntegrationTests.Providers;

public sealed class SqliteProviderFixture : RelationalProviderFixtureBase
{
    private readonly string _directory;
    private readonly string _connectionString;

    public SqliteProviderFixture()
    {
        _directory = Path.Join(Path.GetTempPath(), $"elsa-modular-persistence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _connectionString = $"Data Source={Path.Join(_directory, "modular-persistence.db")}";
    }

    public override string ProviderName => "SQLite";

    public override bool IsAvailable => true;

    public override async ValueTask ResetAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DROP TABLE IF EXISTS ModularPersistenceDocumentIndexes;
            DROP TABLE IF EXISTS ModularPersistenceDocuments;
            DROP TABLE IF EXISTS ModularPersistenceSchemaHistory;
            """;
        await command.ExecuteNonQueryAsync();
    }

    public override async ValueTask MaterializeAsync(StorageManifestDescriptor manifest)
    {
        var materializer = new SqliteDocumentSchemaMaterializer(CreateConnectionFactory());
        await materializer.MaterializeAsync(manifest);
    }

    public override IDocumentStore CreateStore(StorageManifestDescriptor manifest) =>
        new SqliteDocumentStore(CreateConnectionFactory(), manifest);

    public override async ValueTask<bool> TableExistsAsync(string tableName)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @Name";
        command.Parameters.AddWithValue("@Name", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public override async ValueTask<bool> IndexExistsAsync(string indexName)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = @Name";
        command.Parameters.AddWithValue("@Name", indexName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public override async ValueTask<int> CountIndexRowsAsync(DocumentKey key)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM ModularPersistenceDocumentIndexes
            WHERE DocumentId = @Id AND DocumentType = @Type AND TenantId = @TenantId;
            """;
        AddKeyParameters(command, key);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public override async ValueTask<IReadOnlyCollection<(string SchemaName, string Version)>> ReadSchemaHistoryAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SchemaName, Version FROM ModularPersistenceSchemaHistory ORDER BY SchemaName, Version";
        await using var reader = await command.ExecuteReaderAsync();
        var entries = new List<(string SchemaName, string Version)>();

        while (await reader.ReadAsync())
            entries.Add((reader.GetString(0), reader.GetString(1)));

        return entries;
    }

    public override ValueTask DisposeAsync()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);

        return ValueTask.CompletedTask;
    }

    private SqliteModularPersistenceConnectionFactory CreateConnectionFactory() =>
        new(new SqliteModularPersistenceOptions { ConnectionString = _connectionString });
}
