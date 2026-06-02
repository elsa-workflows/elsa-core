namespace Elsa.ModularPersistence.Relational.IntegrationTests.Contracts;

public abstract class DocumentSchemaMaterializerContractTests : IAsyncDisposable
{
    private readonly IRelationalProviderFixture _fixture;

    protected DocumentSchemaMaterializerContractTests()
    {
        _fixture = CreateFixture();
    }

    [Fact]
    public async Task MaterializeAsyncCreatesDocumentTablesAndPortableIndexes()
    {
        if (!ProviderAvailable())
            return;

        var manifest = TestDocuments.CreateManifest();

        await _fixture.ResetAsync();
        await _fixture.MaterializeAsync(manifest);

        Assert.True(await _fixture.TableExistsAsync("ModularPersistenceDocuments"), $"{_fixture.ProviderName} did not create documents table.");
        Assert.True(await _fixture.TableExistsAsync("ModularPersistenceDocumentIndexes"), $"{_fixture.ProviderName} did not create document indexes table.");
        Assert.True(await _fixture.TableExistsAsync("ModularPersistenceSchemaHistory"), $"{_fixture.ProviderName} did not create schema history table.");
        Assert.True(await _fixture.IndexExistsAsync("IX_ModularPersistenceDocumentIndexes_StringValue"), $"{_fixture.ProviderName} did not create string index.");
        Assert.True(await _fixture.IndexExistsAsync("IX_ModularPersistenceDocumentIndexes_NumberValue"), $"{_fixture.ProviderName} did not create number index.");
        Assert.True(await _fixture.IndexExistsAsync("IX_ModularPersistenceDocumentIndexes_DateTimeValue"), $"{_fixture.ProviderName} did not create date-time index.");
    }

    [Fact]
    public async Task MaterializeAsyncRecordsManifestVersionOnce()
    {
        if (!ProviderAvailable())
            return;

        var manifest = TestDocuments.CreateManifest();

        await _fixture.ResetAsync();
        await _fixture.MaterializeAsync(manifest);
        await _fixture.MaterializeAsync(manifest);

        var history = await _fixture.ReadSchemaHistoryAsync();
        var entry = Assert.Single(history);
        Assert.Equal(("sample.secrets", "1.0.0"), entry);
    }

    [Fact]
    public async Task MaterializeAsyncCanRunConcurrentlyWithoutDuplicateHistory()
    {
        if (!ProviderAvailable())
            return;

        var manifest = TestDocuments.CreateManifest();

        await _fixture.ResetAsync();
        await Task.WhenAll(
            _fixture.MaterializeAsync(manifest).AsTask(),
            _fixture.MaterializeAsync(manifest).AsTask());

        var history = await _fixture.ReadSchemaHistoryAsync();
        var entry = Assert.Single(history);
        Assert.Equal(("sample.secrets", "1.0.0"), entry);
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    protected abstract IRelationalProviderFixture CreateFixture();

    private bool ProviderAvailable() => _fixture.IsAvailable;
}
