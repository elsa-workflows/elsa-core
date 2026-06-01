using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Planning;
using Elsa.ModularPersistence.Runtime;
using Elsa.ModularPersistence.Services;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.UnitTests.Runtime;

public class RuntimePhysicalizationOperationsTests
{
    private readonly InMemoryRuntimeStorageDefinitionStore _store = new();
    private readonly RuntimePhysicalizationOperations _operations;

    public RuntimePhysicalizationOperationsTests()
    {
        var materializer = new FakeStorageManifestMaterializer("Portable");
        var capabilities = new StorageProviderCapabilitiesRegistry([new StorageProviderCapabilitiesRegistration("Portable", ProviderCapabilities.PortableDocument)]);
        var manager = new RuntimeStorageDefinitionManager(
            _store,
            [materializer],
            capabilities,
            Microsoft.Extensions.Options.Options.Create(new ModularPersistenceOptions { ProviderName = "Portable" }),
            new StorageManifestMaterializationTracker(),
            new InMemoryRuntimeSchemaAuditTrail());
        _operations = new RuntimePhysicalizationOperations(
            _store,
            manager,
            [new StoragePhysicalizationPlannerRegistration("Portable", ProviderCapabilities.PortableDocument)],
            Microsoft.Extensions.Options.Options.Create(new ModularPersistenceOptions { ProviderName = "Portable" }));
    }

    [Fact]
    public async Task PlanAsyncReturnsProviderPlanForDefinition()
    {
        await _store.SaveAsync(CreateDefinition(RuntimeStorageDefinitionState.Draft));

        var plans = await _operations.PlanAsync("customers");

        var plan = Assert.Single(plans);
        Assert.Equal("Portable", plan.ProviderName);
        Assert.True(plan.IsSupported);
    }

    [Fact]
    public async Task PromoteAndDemoteIndexUpdateDraftPhysicalizationIntent()
    {
        await _store.SaveAsync(CreateDefinition(RuntimeStorageDefinitionState.Draft));

        var promoted = await _operations.PromoteIndexAsync("customers", "IX_Customers_Email");
        var demoted = await _operations.DemoteIndexAsync("customers", "IX_Customers_Email");

        Assert.Equal(PhysicalizationIntent.OptimizedIndexes, Assert.Single(promoted.Indexes).PhysicalizationIntent);
        Assert.Equal(PhysicalizationIntent.PortableDocument, Assert.Single(demoted.Indexes).PhysicalizationIntent);
    }

    [Fact]
    public async Task PromoteRejectsPublishedDefinition()
    {
        await _store.SaveAsync(CreateDefinition(RuntimeStorageDefinitionState.Published));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _operations.PromoteIndexAsync("customers", "IX_Customers_Email"));

        Assert.Contains("Only draft", exception.Message);
    }

    [Fact]
    public async Task PromoteRejectsUnknownIndex()
    {
        await _store.SaveAsync(CreateDefinition(RuntimeStorageDefinitionState.Draft));

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _operations.PromoteIndexAsync("customers", "IX_Customers_Missing"));
    }

    private static RuntimeStorageDefinition CreateDefinition(RuntimeStorageDefinitionState state) =>
        new(
            "customers",
            "runtime.customers",
            "Customers",
            [
                new RuntimeStorageFieldDefinition("Id", StorageFieldType.String, true),
                new RuntimeStorageFieldDefinition("Email", StorageFieldType.String, true)
            ],
            [
                new RuntimeStorageIndexDefinition("IX_Customers_Email", ["Email"])
            ],
            ["read:customers"])
        {
            State = state
        };

    private sealed class FakeStorageManifestMaterializer(string providerName) : IStorageManifestMaterializer
    {
        public string ProviderName { get; } = providerName;

        public bool CanMaterialize(StorageManifestDescriptor manifest) => true;

        public ValueTask MaterializeAsync(StorageManifestDescriptor manifest, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
