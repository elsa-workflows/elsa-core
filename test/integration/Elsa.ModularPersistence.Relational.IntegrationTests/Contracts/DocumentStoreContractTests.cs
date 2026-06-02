using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Queries;

namespace Elsa.ModularPersistence.Relational.IntegrationTests.Contracts;

public abstract class DocumentStoreContractTests : IAsyncDisposable
{
    private readonly IRelationalProviderFixture _fixture;
    private readonly StorageManifestDescriptor _manifest = TestDocuments.CreateManifest();

    protected DocumentStoreContractTests()
    {
        _fixture = CreateFixture();
    }

    [Fact]
    public async Task SaveAsyncPersistsDocumentAndIndexes()
    {
        if (!ProviderAvailable())
            return;

        var store = await ActivateAsync();
        var document = TestDocuments.CreateSecret(
            version: 1,
            data: """{"TenantId":"tenant-a","Name":"Alpha","Priority":42,"ExpiresAt":"2026-01-02T03:04:05+00:00"}""",
            metadata: new Dictionary<string, string> { ["source"] = "test" });

        await using var session = await store.OpenSessionAsync();
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
        Assert.Equal(4, await _fixture.CountIndexRowsAsync(document.Key));
    }

    [Fact]
    public async Task SaveAsyncReplacesDocumentIndexes()
    {
        if (!ProviderAvailable())
            return;

        var store = await ActivateAsync();
        var first = TestDocuments.CreateSecret(version: 1, data: """{"TenantId":"tenant-a","Name":"Alpha","Priority":1,"ExpiresAt":null}""");
        var second = TestDocuments.CreateSecret(version: 2, data: """{"TenantId":"tenant-a","Name":"Beta","Priority":2,"ExpiresAt":null}""");

        await using var session = await store.OpenSessionAsync();
        await session.SaveAsync(first, ExpectedDocumentVersion.New);
        await session.SaveAsync(second, ExpectedDocumentVersion.Exact(1));

        Assert.Equal(4, await _fixture.CountIndexRowsAsync(first.Key));
        var results = await session.QueryAsync(new DocumentQuery(
            "Secrets",
            [
                DocumentQueryFilter.Equal("IX_Secrets_Name", "Name", DocumentQueryValue.String("Beta"))
            ]));
        Assert.Equal("secret-1", Assert.Single(results).Id);
    }

    [Fact]
    public async Task SaveAsyncRejectsExistingAndStaleVersions()
    {
        if (!ProviderAvailable())
            return;

        var store = await ActivateAsync();
        var first = TestDocuments.CreateSecret(version: 1);
        var second = TestDocuments.CreateSecret(version: 2, data: """{"TenantId":"tenant-a","Name":"Beta","Priority":2,"ExpiresAt":null}""");

        await using var session = await store.OpenSessionAsync();
        await session.SaveAsync(first, ExpectedDocumentVersion.New);
        await Assert.ThrowsAsync<DocumentConcurrencyException>(async () => await session.SaveAsync(first, ExpectedDocumentVersion.New));

        await session.SaveAsync(second, ExpectedDocumentVersion.Exact(1));
        await Assert.ThrowsAsync<DocumentConcurrencyException>(async () => await session.SaveAsync(second, ExpectedDocumentVersion.Exact(1)));
    }

    [Fact]
    public async Task DeleteAsyncRemovesDocumentAndIndexes()
    {
        if (!ProviderAvailable())
            return;

        var store = await ActivateAsync();
        var document = TestDocuments.CreateSecret(version: 1);

        await using var session = await store.OpenSessionAsync();
        await session.SaveAsync(document, ExpectedDocumentVersion.New);
        await session.DeleteAsync(document.Key, ExpectedDocumentVersion.Exact(1));

        Assert.Null(await session.LoadAsync(document.Key));
        Assert.Equal(0, await _fixture.CountIndexRowsAsync(document.Key));
    }

    [Fact]
    public async Task DeleteAsyncRejectsStaleExactVersion()
    {
        if (!ProviderAvailable())
            return;

        var store = await ActivateAsync();
        var document = TestDocuments.CreateSecret(version: 1);

        await using var session = await store.OpenSessionAsync();
        await session.SaveAsync(document, ExpectedDocumentVersion.New);

        await Assert.ThrowsAsync<DocumentConcurrencyException>(async () => await session.DeleteAsync(document.Key, ExpectedDocumentVersion.Exact(2)));
        Assert.NotNull(await session.LoadAsync(document.Key));
        Assert.Equal(4, await _fixture.CountIndexRowsAsync(document.Key));
    }

    [Fact]
    public async Task ConcurrentExactVersionUpdatesHaveOneWinner()
    {
        if (!ProviderAvailable())
            return;

        var store = await ActivateAsync();
        var first = TestDocuments.CreateSecret(version: 1);
        var updateA = TestDocuments.CreateSecret(version: 2, data: """{"TenantId":"tenant-a","Name":"Beta","Priority":2,"ExpiresAt":null}""");
        var updateB = TestDocuments.CreateSecret(version: 3, data: """{"TenantId":"tenant-a","Name":"Gamma","Priority":3,"ExpiresAt":null}""");

        await using var seedSession = await store.OpenSessionAsync();
        await seedSession.SaveAsync(first, ExpectedDocumentVersion.New);

        var results = await Task.WhenAll(SaveAndCaptureAsync(store, updateA), SaveAndCaptureAsync(store, updateB));

        Assert.Single(results, x => x is null);
        Assert.Single(results, x => x is DocumentConcurrencyException);
    }

    [Fact]
    public async Task QueryAsyncFiltersEqualsInRangeNullSortsAndPages()
    {
        if (!ProviderAvailable())
            return;

        var store = await ActivateAsync();

        await using var session = await store.OpenSessionAsync();
        await session.SaveAsync(TestDocuments.CreateSecret("secret-1", 1, """{"TenantId":"tenant-a","Name":"Alpha","Priority":1,"ExpiresAt":null}"""));
        await session.SaveAsync(TestDocuments.CreateSecret("secret-2", 1, """{"TenantId":"tenant-a","Name":"Beta","Priority":2,"ExpiresAt":"2026-02-01T00:00:00+00:00"}"""));
        await session.SaveAsync(TestDocuments.CreateSecret("secret-3", 1, """{"TenantId":"tenant-a","Name":"Gamma","Priority":3,"ExpiresAt":"2026-03-01T00:00:00+00:00"}"""));

        var equalsInQuery = new DocumentQuery(
            "Secrets",
            [
                DocumentQueryFilter.Equal("IX_Secrets_TenantId", "TenantId", DocumentQueryValue.String("tenant-a")),
                DocumentQueryFilter.In(
                    "IX_Secrets_Name",
                    "Name",
                    [
                        DocumentQueryValue.String("Alpha"),
                        DocumentQueryValue.String("Gamma")
                    ])
            ],
            [
                new DocumentQuerySort("IX_Secrets_Priority", "Priority", StorageIndexSortOrder.Descending)
            ],
            new DocumentQueryPage(1));
        var rangeQuery = new DocumentQuery(
            "Secrets",
            [
                DocumentQueryFilter.Between("IX_Secrets_Priority", "Priority", DocumentQueryValue.Int32(2), DocumentQueryValue.Int32(3))
            ],
            [
                new DocumentQuerySort("IX_Secrets_Priority", "Priority")
            ],
            new DocumentQueryPage(1, 1));
        var nullQuery = new DocumentQuery(
            "Secrets",
            [
                DocumentQueryFilter.IsNull("IX_Secrets_ExpiresAt", "ExpiresAt")
            ]);

        Assert.Equal("secret-3", Assert.Single(await session.QueryAsync(equalsInQuery)).Id);
        Assert.Equal("secret-3", Assert.Single(await session.QueryAsync(rangeQuery)).Id);
        Assert.Equal("secret-1", Assert.Single(await session.QueryAsync(nullQuery)).Id);
    }

    [Fact]
    public async Task QueryAsyncRejectsUnsupportedAndUndeclaredIndexQueries()
    {
        if (!ProviderAvailable())
            return;

        var store = await ActivateAsync();

        await using var session = await store.OpenSessionAsync();
        var unsupported = new DocumentQuery(
            "Secrets",
            [
                DocumentQueryFilter.StartsWith("IX_Secrets_Name", "Name", DocumentQueryValue.String("A"))
            ]);
        var missingIndex = new DocumentQuery(
            "Secrets",
            [
                DocumentQueryFilter.Equal("IX_Secrets_Missing", "Name", DocumentQueryValue.String("Alpha"))
            ]);

        var unsupportedException = await Assert.ThrowsAsync<DocumentQueryException>(async () => await session.QueryAsync(unsupported));
        var missingIndexException = await Assert.ThrowsAsync<DocumentQueryException>(async () => await session.QueryAsync(missingIndex));

        Assert.Contains(unsupportedException.Plan.Diagnostics, x => x.Code == "UnsupportedQueryOperator");
        Assert.Contains(missingIndexException.Plan.Diagnostics, x => x.Code == "UnknownIndex");
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    protected abstract IRelationalProviderFixture CreateFixture();

    private bool ProviderAvailable() => _fixture.IsAvailable;

    private async ValueTask<IDocumentStore> ActivateAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.MaterializeAsync(_manifest);
        return _fixture.CreateStore(_manifest);
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
}
