using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.UnitTests.Validation;

public class StorageManifestValidatorTests
{
    private readonly StorageManifestValidator _validator = new();

    [Fact]
    public void ValidatesPortableDocumentManifest()
    {
        var manifest = CreateManifest();

        var result = _validator.Validate(manifest, ProviderCapabilities.PortableDocument);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReportsUnsupportedStorageUnitKind()
    {
        var manifest = CreateManifest(kind: StorageUnitKind.Relational);

        var result = _validator.Validate(manifest, ProviderCapabilities.PortableDocument);

        var error = Assert.Single(result.Errors);
        Assert.False(result.IsValid);
        Assert.Equal("UnsupportedStorageUnitKind", error.Code);
        Assert.Equal("storageUnits['Secrets'].kind", error.Path);
    }

    [Fact]
    public void ReportsUnsupportedFieldType()
    {
        var manifest = CreateManifest(fieldType: StorageFieldType.Binary);
        var capabilities = new ProviderCapabilities(
            [StorageUnitKind.Document],
            [StorageFieldType.String, StorageFieldType.Json],
            [PhysicalizationIntent.PortableDocument]);

        var result = _validator.Validate(manifest, capabilities);

        var error = Assert.Single(result.Errors);
        Assert.Equal("UnsupportedFieldType", error.Code);
        Assert.Equal("storageUnits['Secrets'].fields['Value'].type", error.Path);
    }

    [Fact]
    public void ReportsUnsupportedStorageUnitPhysicalizationIntent()
    {
        var manifest = CreateManifest(physicalizationIntent: PhysicalizationIntent.NativePhysicalized);

        var result = _validator.Validate(manifest, ProviderCapabilities.PortableDocument);

        var error = Assert.Single(result.Errors);
        Assert.Equal("UnsupportedPhysicalizationIntent", error.Code);
        Assert.Equal("storageUnits['Secrets'].physicalizationIntent", error.Path);
    }

    [Fact]
    public void ReportsUnsupportedIndexPhysicalizationIntent()
    {
        var manifest = CreateManifest(indexPhysicalizationIntent: PhysicalizationIntent.OptimizedIndexes);

        var result = _validator.Validate(manifest, ProviderCapabilities.PortableDocument);

        var error = Assert.Single(result.Errors);
        Assert.Equal("UnsupportedPhysicalizationIntent", error.Code);
        Assert.Equal("storageUnits['Secrets'].indexes['IX_Secrets_Value'].physicalizationIntent", error.Path);
    }

    [Fact]
    public void ReportsMissingPrimaryKey()
    {
        var manifest = new StorageManifestDescriptor(
            "sample.secrets",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Secrets",
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true)
                    ],
                    keys: [])
            ]);

        var result = _validator.Validate(manifest, ProviderCapabilities.PortableDocument);

        var error = Assert.Single(result.Errors);
        Assert.Equal("MissingPrimaryKey", error.Code);
        Assert.Equal("storageUnits['Secrets'].keys", error.Path);
    }

    [Fact]
    public void ProviderCapabilitiesRequirePositiveIndexFieldLimit()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ProviderCapabilities(
            [StorageUnitKind.Document],
            Enum.GetValues<StorageFieldType>(),
            [PhysicalizationIntent.PortableDocument],
            maxIndexFieldCount: 0));

        Assert.Equal("maxIndexFieldCount", exception.ParamName);
    }

    [Fact]
    public void ReportsIndexFieldCountBeyondProviderLimit()
    {
        var manifest = new StorageManifestDescriptor(
            "sample.secrets",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Secrets",
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true),
                        new StorageFieldDescriptor("TenantId", StorageFieldType.String),
                        new StorageFieldDescriptor("Name", StorageFieldType.String)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Secrets", ["Id"])
                    ],
                    [
                        new StorageIndexDescriptor(
                            "IX_Secrets_TenantId_Name",
                            [
                                new StorageIndexFieldDescriptor("TenantId"),
                                new StorageIndexFieldDescriptor("Name")
                            ])
                    ])
            ]);
        var capabilities = new ProviderCapabilities(
            [StorageUnitKind.Document],
            Enum.GetValues<StorageFieldType>(),
            [PhysicalizationIntent.PortableDocument],
            maxIndexFieldCount: 1);

        var result = _validator.Validate(manifest, capabilities);

        var error = Assert.Single(result.Errors);
        Assert.Equal("TooManyIndexFields", error.Code);
        Assert.Equal("storageUnits['Secrets'].indexes['IX_Secrets_TenantId_Name'].fields", error.Path);
    }

    private static StorageManifestDescriptor CreateManifest(
        StorageUnitKind kind = StorageUnitKind.Document,
        StorageFieldType fieldType = StorageFieldType.Json,
        PhysicalizationIntent physicalizationIntent = PhysicalizationIntent.PortableDocument,
        PhysicalizationIntent indexPhysicalizationIntent = PhysicalizationIntent.PortableDocument) =>
        new(
            "sample.secrets",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Secrets",
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Value", fieldType, true)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Secrets", ["Id"])
                    ],
                    [
                        new StorageIndexDescriptor("IX_Secrets_Value", [new StorageIndexFieldDescriptor("Value")], physicalizationIntent: indexPhysicalizationIntent)
                    ],
                    physicalizationIntent,
                    kind)
            ]);
}
