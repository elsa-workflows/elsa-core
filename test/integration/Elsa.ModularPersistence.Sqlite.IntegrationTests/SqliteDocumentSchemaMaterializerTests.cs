using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Sqlite.Options;
using Elsa.ModularPersistence.Sqlite.Services;
using Microsoft.Data.Sqlite;

namespace Elsa.ModularPersistence.Sqlite.IntegrationTests;

public class SqliteDocumentSchemaMaterializerTests : IAsyncDisposable
{
    private readonly string _directory;
    private readonly string _connectionString;
    private readonly SqliteDocumentSchemaMaterializer _materializer;

    public SqliteDocumentSchemaMaterializerTests()
    {
        _directory = Path.Join(Path.GetTempPath(), $"elsa-modular-persistence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _connectionString = $"Data Source={Path.Join(_directory, "modular-persistence.db")}";
        var connectionFactory = new SqliteModularPersistenceConnectionFactory(new SqliteModularPersistenceOptions { ConnectionString = _connectionString });
        _materializer = new SqliteDocumentSchemaMaterializer(connectionFactory);
    }

    [Fact]
    public async Task MaterializeAsyncCreatesDocumentTablesAndIndexes()
    {
        await _materializer.MaterializeAsync(CreateManifest());

        Assert.True(await TableExistsAsync("ModularPersistenceDocuments"));
        Assert.True(await TableExistsAsync("ModularPersistenceDocumentIndexes"));
        Assert.True(await TableExistsAsync("ModularPersistenceSchemaHistory"));
        Assert.True(await IndexExistsAsync("IX_ModularPersistenceDocumentIndexes_StringValue"));
        Assert.True(await IndexExistsAsync("IX_ModularPersistenceDocumentIndexes_NumberValue"));
        Assert.True(await IndexExistsAsync("IX_ModularPersistenceDocumentIndexes_DateTimeValue"));
    }

    [Fact]
    public async Task MaterializeAsyncRecordsManifestVersion()
    {
        await _materializer.MaterializeAsync(CreateManifest());

        var history = await ReadSchemaHistoryAsync();

        var entry = Assert.Single(history);
        Assert.Equal(("sample.secrets", "1.0.0"), entry);
    }

    [Fact]
    public async Task MaterializeAsyncIsIdempotent()
    {
        var manifest = CreateManifest();

        await _materializer.MaterializeAsync(manifest);
        await _materializer.MaterializeAsync(manifest);

        Assert.Equal(1, await CountRowsAsync("ModularPersistenceSchemaHistory"));
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);

        return ValueTask.CompletedTask;
    }

    private async ValueTask<bool> TableExistsAsync(string tableName)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @Name";
        command.Parameters.AddWithValue("@Name", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    private async ValueTask<bool> IndexExistsAsync(string indexName)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = @Name";
        command.Parameters.AddWithValue("@Name", indexName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    private async ValueTask<int> CountRowsAsync(string tableName)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private async ValueTask<IReadOnlyCollection<(string SchemaName, string Version)>> ReadSchemaHistoryAsync()
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

    private static StorageManifestDescriptor CreateManifest() =>
        new(
            "sample.secrets",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Secrets",
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true),
                        new StorageFieldDescriptor("TenantId", StorageFieldType.String),
                        new StorageFieldDescriptor("Value", StorageFieldType.Json, true)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Secrets", ["Id"])
                    ],
                    [
                        new StorageIndexDescriptor("IX_Secrets_TenantId", [new StorageIndexFieldDescriptor("TenantId")])
                    ])
            ]);
}
