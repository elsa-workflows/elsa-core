using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Runtime;
using Elsa.ModularPersistence.Services;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.UnitTests.Runtime;

public class RuntimeStorageDefinitionManagerTests
{
    private readonly InMemoryRuntimeStorageDefinitionStore _store = new();
    private readonly FakeStorageManifestMaterializer _materializer = new("Fake");
    private readonly StorageManifestMaterializationTracker _tracker = new();
    private readonly InMemoryRuntimeSchemaAuditTrail _auditTrail = new();
    private readonly RuntimeStorageDefinitionManager _manager;

    public RuntimeStorageDefinitionManagerTests()
    {
        var capabilities = new StorageProviderCapabilitiesRegistry(
            [
                new StorageProviderCapabilitiesRegistration("Fake", ProviderCapabilities.PortableDocument)
            ]);
        _manager = new RuntimeStorageDefinitionManager(
            _store,
            [_materializer],
            capabilities,
            Microsoft.Extensions.Options.Options.Create(new ModularPersistenceOptions { ProviderName = "Fake" }),
            _tracker,
            _auditTrail);
    }

    [Fact]
    public async Task SaveDraftAsyncStoresDraftDefinition()
    {
        var definition = CreateDefinition();

        var saved = await _manager.SaveDraftAsync(definition);
        var loaded = await _manager.GetAsync(definition.Id);

        Assert.Same(definition, saved);
        Assert.Same(definition, loaded);
        Assert.Equal(RuntimeStorageDefinitionState.Draft, loaded?.State);
        var audit = Assert.Single(await _auditTrail.ListAsync(definition.Id));
        Assert.Equal(RuntimeSchemaAuditAction.DraftSaved, audit.Action);
        Assert.True(audit.Succeeded);
    }

    [Fact]
    public async Task PublishAsyncValidatesMaterializesAndMarksDefinitionPublished()
    {
        var definition = CreateDefinition(indexes: [new RuntimeStorageIndexDefinition("IX_Customers_Email", ["Email"], true)]);
        await _manager.SaveDraftAsync(definition);

        var result = await _manager.PublishAsync(definition.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Manifest);
        Assert.Equal(RuntimeStorageDefinitionState.Published, result.Definition.State);
        Assert.Equal(RuntimeStorageDefinitionState.Published, (await _manager.GetAsync(definition.Id))?.State);
        var manifest = Assert.Single(_materializer.MaterializedManifests);
        Assert.Equal("runtime.customers", manifest.SchemaName);
        var unit = Assert.Single(manifest.StorageUnits);
        Assert.Equal("Customers", unit.Name);
        Assert.Contains(unit.Keys, x => x.Kind == StorageKeyKind.Primary && x.FieldNames.SequenceEqual(["Id"]));
        Assert.Equal("IX_Customers_Email", Assert.Single(unit.Indexes).Name);
        var record = Assert.Single(_tracker.Records);
        Assert.True(record.Succeeded);
        Assert.Equal("Fake", record.ProviderName);
        var audit = (await _auditTrail.ListAsync(definition.Id)).Last();
        Assert.Equal(RuntimeSchemaAuditAction.Published, audit.Action);
        Assert.Equal(RuntimeStorageDefinitionState.Draft, audit.Before?.State);
        Assert.Equal(RuntimeStorageDefinitionState.Published, audit.After?.State);
    }

    [Fact]
    public async Task SaveDraftAsyncRejectsOverwriteOfPublishedDefinition()
    {
        var definition = CreateDefinition();
        await _manager.SaveDraftAsync(definition);
        await _manager.PublishAsync(definition.Id);
        var replacementDraft = CreateDefinition();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _manager.SaveDraftAsync(replacementDraft));

        Assert.Contains("Published", exception.Message);
        Assert.Equal(RuntimeStorageDefinitionState.Published, (await _manager.GetAsync(definition.Id))?.State);
    }

    [Fact]
    public async Task PublishAsyncRejectsInvalidDraftWithoutMaterializing()
    {
        var definition = new RuntimeStorageDefinition(
            "bad",
            "runtime.bad",
            "Bad",
            [new RuntimeStorageFieldDefinition("Name", StorageFieldType.String)],
            requiredPermissions: ["read:bad"]);
        await _manager.SaveDraftAsync(definition);

        var result = await _manager.PublishAsync(definition.Id);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, x => x.Code == "MissingIdField");
        Assert.Empty(_materializer.MaterializedManifests);
        Assert.Equal(RuntimeStorageDefinitionState.Draft, (await _manager.GetAsync(definition.Id))?.State);
    }

    [Fact]
    public async Task PublishAsyncRejectsUnsupportedProviderCapabilities()
    {
        var definition = CreateDefinition(indexes: [new RuntimeStorageIndexDefinition("IX_Customers_Email", ["Email"], physicalizationIntent: PhysicalizationIntent.OptimizedIndexes)]);
        await _manager.SaveDraftAsync(definition);

        var result = await _manager.PublishAsync(definition.Id);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, x => x.Code == "UnsupportedPhysicalizationIntent" && x.Path.Contains("Fake", StringComparison.Ordinal));
        Assert.Empty(_materializer.MaterializedManifests);
    }

    [Fact]
    public async Task RetireAsyncPreventsPublishAndRematerialize()
    {
        var definition = CreateDefinition();
        await _manager.SaveDraftAsync(definition);
        await _manager.PublishAsync(definition.Id);
        var retired = await _manager.RetireAsync(definition.Id);

        var publishResult = await _manager.PublishAsync(definition.Id);
        var rematerializeResult = await _manager.RematerializeAsync(definition.Id);

        Assert.Equal(RuntimeStorageDefinitionState.Retired, retired.State);
        Assert.False(publishResult.Succeeded);
        Assert.False(rematerializeResult.Succeeded);
        Assert.All(publishResult.Errors.Concat(rematerializeResult.Errors), x => Assert.Equal("InvalidLifecycleState", x.Code));
        Assert.Single(_materializer.MaterializedManifests);
    }

    [Fact]
    public async Task RematerializeAsyncAppliesPublishedDefinition()
    {
        var definition = CreateDefinition();
        await _manager.SaveDraftAsync(definition);
        await _manager.PublishAsync(definition.Id);

        var result = await _manager.RematerializeAsync(definition.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(2, _materializer.MaterializedManifests.Count);
        Assert.Equal(2, _tracker.Records.Count);
    }

    [Fact]
    public async Task RetireAsyncRejectsDraftDefinition()
    {
        var definition = CreateDefinition();
        await _manager.SaveDraftAsync(definition);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _manager.RetireAsync(definition.Id));

        Assert.Contains("Draft", exception.Message);
    }

    private static RuntimeStorageDefinition CreateDefinition(IEnumerable<RuntimeStorageIndexDefinition>? indexes = null) =>
        new(
            "customers",
            "runtime.customers",
            "Customers",
            [
                new RuntimeStorageFieldDefinition("Id", StorageFieldType.String, true),
                new RuntimeStorageFieldDefinition("Email", StorageFieldType.String, true)
            ],
            indexes,
            ["read:customers", "write:customers"]);

    private sealed class FakeStorageManifestMaterializer(string providerName) : IStorageManifestMaterializer
    {
        public List<StorageManifestDescriptor> MaterializedManifests { get; } = [];

        public string ProviderName { get; } = providerName;

        public bool CanMaterialize(StorageManifestDescriptor manifest) => true;

        public ValueTask MaterializeAsync(StorageManifestDescriptor manifest, CancellationToken cancellationToken = default)
        {
            MaterializedManifests.Add(manifest);
            return ValueTask.CompletedTask;
        }
    }
}
