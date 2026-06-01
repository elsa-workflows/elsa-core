using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Queries;

namespace Elsa.ModularPersistence.MongoDb.IntegrationTests;

public class MongoDbDocumentStoreContractTests : IAsyncDisposable
{
    private readonly MongoDbIntegrationFixture _fixture = new();
    private readonly StorageManifestDescriptor _manifest = CreateManifest();

    [Fact]
    public async Task MaterializeAsyncCreatesNativeDeclaredIndexes()
    {
        if (!_fixture.IsAvailable)
            return;

        await _fixture.MaterializeAsync(_manifest);

        var indexNames = await _fixture.ReadIndexNamesAsync("ModularPersistenceDocuments");

        Assert.Contains("IX_ModularPersistenceDocuments_Key", indexNames);
        Assert.Contains("IX_ModularPersistenceDocuments_Secrets_IX_Secrets_Name", indexNames);
        Assert.Contains("IX_ModularPersistenceDocuments_Secrets_IX_Secrets_Priority", indexNames);
    }

    [Fact]
    public async Task SaveLoadAndQueryUseMongoDocumentShape()
    {
        if (!_fixture.IsAvailable)
            return;

        await _fixture.MaterializeAsync(_manifest);
        var store = _fixture.CreateStore(_manifest);
        var document = CreateDocument(
            "secret-1",
            1,
            """{"TenantId":"tenant-a","Name":"Alpha","Priority":42,"ExpiresAt":null}""",
            new Dictionary<string, string> { ["source"] = "test" });

        await using var session = await store.OpenSessionAsync();
        await session.SaveAsync(document, ExpectedDocumentVersion.New);

        var loaded = await session.LoadAsync(document.Key);
        var results = await session.QueryAsync(new DocumentQuery(
            "Secrets",
            [
                DocumentQueryFilter.Equal("IX_Secrets_Name", "Name", DocumentQueryValue.String("Alpha"))
            ]));

        Assert.NotNull(loaded);
        Assert.Equal(document.Data, loaded.Data);
        Assert.Equal("test", loaded.Metadata["source"]);
        Assert.Equal("secret-1", Assert.Single(results).Id);
    }

    [Fact]
    public async Task ConcurrentExactVersionUpdatesHaveOneWinner()
    {
        if (!_fixture.IsAvailable)
            return;

        await _fixture.MaterializeAsync(_manifest);
        var store = _fixture.CreateStore(_manifest);

        await using var seedSession = await store.OpenSessionAsync();
        await seedSession.SaveAsync(CreateDocument("secret-1", 1, """{"TenantId":"tenant-a","Name":"Alpha","Priority":1,"ExpiresAt":null}"""), ExpectedDocumentVersion.New);

        var results = await Task.WhenAll(
            SaveAndCaptureAsync(store, CreateDocument("secret-1", 2, """{"TenantId":"tenant-a","Name":"Beta","Priority":2,"ExpiresAt":null}""")),
            SaveAndCaptureAsync(store, CreateDocument("secret-1", 3, """{"TenantId":"tenant-a","Name":"Gamma","Priority":3,"ExpiresAt":null}""")));

        Assert.Single(results, x => x is null);
        Assert.Single(results, x => x is DocumentConcurrencyException);
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    private static async Task<Exception?> SaveAndCaptureAsync(IDocumentStore store, DocumentEnvelope document)
    {
        try
        {
            await using var session = await store.OpenSessionAsync();
            await session.SaveAsync(document, ExpectedDocumentVersion.Exact(1));
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    private static DocumentEnvelope CreateDocument(
        string id,
        long version,
        string data,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            id,
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
                        new StorageIndexDescriptor("IX_Secrets_TenantId", [new StorageIndexFieldDescriptor("TenantId")]),
                        new StorageIndexDescriptor("IX_Secrets_Name", [new StorageIndexFieldDescriptor("Name")]),
                        new StorageIndexDescriptor("IX_Secrets_Priority", [new StorageIndexFieldDescriptor("Priority")]),
                        new StorageIndexDescriptor("IX_Secrets_ExpiresAt", [new StorageIndexFieldDescriptor("ExpiresAt")])
                    ])
            ]);
}
