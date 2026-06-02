using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Relational.Contracts;
using Elsa.ModularPersistence.Sqlite.Options;
using Elsa.ModularPersistence.Sqlite.Services;
using Microsoft.Data.Sqlite;

namespace Elsa.ModularPersistence.Sqlite.IntegrationTests;

public class SqliteDocumentStoreTests : IAsyncDisposable
{
    private readonly string _directory;
    private readonly string _connectionString;
    private readonly StorageManifestDescriptor _manifest;
    private readonly SqliteDocumentStore _store;

    public SqliteDocumentStoreTests()
    {
        _directory = Path.Join(Path.GetTempPath(), $"elsa-modular-persistence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _connectionString = $"Data Source={Path.Join(_directory, "modular-persistence.db")}";

        _manifest = CreateManifest();
        var connectionFactory = CreateConnectionFactory();
        _store = new SqliteDocumentStore(connectionFactory, _manifest);
    }

    [Fact]
    public async Task SaveAsyncPersistsDocumentAndIndexes()
    {
        await MaterializeAsync();
        var document = CreateDocument(
            version: 1,
            data: """{"TenantId":"tenant-a","Name":"Alpha","Priority":42,"ExpiresAt":"2026-01-02T03:04:05+00:00"}""",
            metadata: new Dictionary<string, string> { ["source"] = "test" });

        await using var session = await _store.OpenSessionAsync();
        var result = await session.SaveAsync(document, ExpectedDocumentVersion.New);

        var loaded = await session.LoadAsync(document.Key);
        Assert.Equal(document.Key, result.Key);
        Assert.Equal(1, result.Version);
        Assert.NotNull(loaded);
        Assert.Equal(document.Id, loaded.Id);
        Assert.Equal(document.Type, loaded.Type);
        Assert.Equal(document.TenantId, loaded.TenantId);
        Assert.Equal(document.Version, loaded.Version);
        Assert.Equal(document.Data, loaded.Data);
        Assert.Equal("test", loaded.Metadata["source"]);
        Assert.Equal(3, await CountIndexRowsAsync(document.Key));
        Assert.True(await IndexValueExistsAsync("IX_Secrets_Name", "Name", stringValue: "Alpha"));
        Assert.True(await IndexValueExistsAsync("IX_Secrets_Priority", "Priority", numberValue: 42));
        Assert.True(await IndexValueExistsAsync("IX_Secrets_ExpiresAt", "ExpiresAt", dateTimeValue: "2026-01-02T03:04:05.0000000+00:00"));
    }

    [Fact]
    public async Task SaveAsyncReplacesDocumentIndexes()
    {
        await MaterializeAsync();
        var first = CreateDocument(version: 1, data: """{"TenantId":"tenant-a","Name":"Alpha","Priority":1,"ExpiresAt":null}""");
        var second = CreateDocument(version: 2, data: """{"TenantId":"tenant-a","Name":"Beta","Priority":2,"ExpiresAt":null}""");

        await using var session = await _store.OpenSessionAsync();
        await session.SaveAsync(first, ExpectedDocumentVersion.New);
        await session.SaveAsync(second, ExpectedDocumentVersion.Exact(1));

        Assert.Equal(3, await CountIndexRowsAsync(first.Key));
        Assert.False(await IndexValueExistsAsync("IX_Secrets_Name", "Name", stringValue: "Alpha"));
        Assert.True(await IndexValueExistsAsync("IX_Secrets_Name", "Name", stringValue: "Beta"));
        Assert.True(await IndexValueExistsAsync("IX_Secrets_Priority", "Priority", numberValue: 2));
    }

    [Fact]
    public async Task SaveAsyncRejectsExistingDocumentWhenExpectedNew()
    {
        await MaterializeAsync();
        var document = CreateDocument(version: 1);

        await using var session = await _store.OpenSessionAsync();
        await session.SaveAsync(document, ExpectedDocumentVersion.New);

        await Assert.ThrowsAsync<DocumentConcurrencyException>(async () => await session.SaveAsync(document, ExpectedDocumentVersion.New));
    }

    [Fact]
    public async Task SaveAsyncRejectsStaleExactVersion()
    {
        await MaterializeAsync();
        var first = CreateDocument(version: 1);
        var second = CreateDocument(version: 2, data: """{"TenantId":"tenant-a","Name":"Beta","Priority":2,"ExpiresAt":null}""");

        await using var session = await _store.OpenSessionAsync();
        await session.SaveAsync(first, ExpectedDocumentVersion.New);
        await session.SaveAsync(second, ExpectedDocumentVersion.Exact(1));

        await Assert.ThrowsAsync<DocumentConcurrencyException>(async () => await session.SaveAsync(second, ExpectedDocumentVersion.Exact(1)));
    }

    [Fact]
    public async Task DeleteAsyncRemovesDocumentAndIndexes()
    {
        await MaterializeAsync();
        var document = CreateDocument(version: 1);

        await using var session = await _store.OpenSessionAsync();
        await session.SaveAsync(document, ExpectedDocumentVersion.New);
        await session.DeleteAsync(document.Key, ExpectedDocumentVersion.Exact(1));

        Assert.Null(await session.LoadAsync(document.Key));
        Assert.Equal(0, await CountIndexRowsAsync(document.Key));
    }

    [Fact]
    public async Task DeleteAsyncRejectsStaleExactVersion()
    {
        await MaterializeAsync();
        var document = CreateDocument(version: 1);

        await using var session = await _store.OpenSessionAsync();
        await session.SaveAsync(document, ExpectedDocumentVersion.New);

        await Assert.ThrowsAsync<DocumentConcurrencyException>(async () => await session.DeleteAsync(document.Key, ExpectedDocumentVersion.Exact(2)));
        Assert.NotNull(await session.LoadAsync(document.Key));
        Assert.Equal(3, await CountIndexRowsAsync(document.Key));
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);

        return ValueTask.CompletedTask;
    }

    private async ValueTask MaterializeAsync()
    {
        var materializer = new SqliteDocumentSchemaMaterializer(CreateConnectionFactory());
        await materializer.MaterializeAsync(_manifest);
    }

    private IRelationalConnectionFactory CreateConnectionFactory() =>
        new SqliteModularPersistenceConnectionFactory(new SqliteModularPersistenceOptions { ConnectionString = _connectionString });

    private async ValueTask<int> CountIndexRowsAsync(DocumentKey key)
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

    private async ValueTask<bool> IndexValueExistsAsync(string indexName, string fieldName, string? stringValue = null, double? numberValue = null, string? dateTimeValue = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM ModularPersistenceDocumentIndexes
            WHERE IndexName = @IndexName
              AND FieldName = @FieldName
              AND (@StringValue IS NULL OR StringValue = @StringValue)
              AND (@NumberValue IS NULL OR NumberValue = @NumberValue)
              AND (@DateTimeValue IS NULL OR DateTimeValue = @DateTimeValue);
            """;
        command.Parameters.AddWithValue("@IndexName", indexName);
        command.Parameters.AddWithValue("@FieldName", fieldName);
        command.Parameters.AddWithValue("@StringValue", (object?)stringValue ?? DBNull.Value);
        command.Parameters.AddWithValue("@NumberValue", (object?)numberValue ?? DBNull.Value);
        command.Parameters.AddWithValue("@DateTimeValue", (object?)dateTimeValue ?? DBNull.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    private static void AddKeyParameters(SqliteCommand command, DocumentKey key)
    {
        command.Parameters.AddWithValue("@Id", key.Id);
        command.Parameters.AddWithValue("@Type", key.Type);
        command.Parameters.AddWithValue("@TenantId", key.TenantId ?? "");
    }

    private static DocumentEnvelope CreateDocument(
        long version,
        string data = """{"TenantId":"tenant-a","Name":"Alpha","Priority":1,"ExpiresAt":null}""",
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            "secret-1",
            "Secrets",
            "tenant-a",
            version,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 0, 0, (int)version, TimeSpan.Zero),
            data,
            metadata);

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
                        new StorageFieldDescriptor("Name", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Priority", StorageFieldType.Int32),
                        new StorageFieldDescriptor("ExpiresAt", StorageFieldType.DateTimeOffset)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Secrets", ["Id"])
                    ],
                    [
                        new StorageIndexDescriptor("IX_Secrets_Name", [new StorageIndexFieldDescriptor("Name")]),
                        new StorageIndexDescriptor("IX_Secrets_Priority", [new StorageIndexFieldDescriptor("Priority")]),
                        new StorageIndexDescriptor("IX_Secrets_ExpiresAt", [new StorageIndexFieldDescriptor("ExpiresAt")])
                    ])
            ]);
}
