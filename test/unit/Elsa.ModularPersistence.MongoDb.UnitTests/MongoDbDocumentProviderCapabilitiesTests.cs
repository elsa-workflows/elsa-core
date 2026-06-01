using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.MongoDb.Options;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.MongoDb.UnitTests;

public class MongoDbDocumentProviderCapabilitiesTests
{
    [Fact]
    public void CapabilitiesAllowOptimizedIndexes()
    {
        var manifest = CreateManifest(PhysicalizationIntent.OptimizedIndexes);

        var result = new StorageManifestValidator().Validate(manifest, MongoDbDocumentProviderCapabilities.Value);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CapabilitiesRejectNativePhysicalization()
    {
        var manifest = CreateManifest(PhysicalizationIntent.NativePhysicalized);

        var result = new StorageManifestValidator().Validate(manifest, MongoDbDocumentProviderCapabilities.Value);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Code == "UnsupportedPhysicalizationIntent");
    }

    [Fact]
    public void TransactionsAreDisabledByDefault()
    {
        var options = new MongoDbModularPersistenceOptions();

        Assert.Equal(MongoDbTransactionMode.Disabled, options.TransactionMode);
    }

    private static StorageManifestDescriptor CreateManifest(PhysicalizationIntent indexIntent) =>
        new(
            "sample.secrets",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Secrets",
                    [
                        new StorageFieldDescriptor("Name", StorageFieldType.String, true)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Secrets", ["Name"])
                    ],
                    [
                        new StorageIndexDescriptor("IX_Secrets_Name", [new StorageIndexFieldDescriptor("Name")], physicalizationIntent: indexIntent)
                    ])
            ]);
}
