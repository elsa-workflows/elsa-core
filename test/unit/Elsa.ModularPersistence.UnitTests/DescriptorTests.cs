using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.UnitTests;

public class DescriptorTests
{
    private readonly StorageFieldDescriptor _id = new("Id", StorageFieldType.String, true);
    private readonly StorageFieldDescriptor _tenantId = new("TenantId", StorageFieldType.String);
    private readonly StorageFieldDescriptor _data = new("Data", StorageFieldType.Json, true);

    [Fact]
    public void CanCreatePortableDocumentManifest()
    {
        var unit = new StorageUnitDescriptor(
            "Secrets",
            [_id, _tenantId, _data],
            [new StorageKeyDescriptor("PK_Secrets", ["Id"])],
            [new StorageIndexDescriptor("IX_Secrets_TenantId", [new StorageIndexFieldDescriptor("TenantId")])]);

        var manifest = new StorageManifestDescriptor("sample.secrets", new StorageManifestVersion(1), [unit]);

        Assert.Equal("sample.secrets", manifest.SchemaName);
        Assert.Equal("1.0.0", manifest.Version.ToString());
        Assert.Equal(StorageUnitKind.Document, manifest.StorageUnits.Single().Kind);
        Assert.Equal(PhysicalizationIntent.PortableDocument, manifest.StorageUnits.Single().PhysicalizationIntent);
        Assert.Equal(PhysicalizationIntent.PortableDocument, manifest.StorageUnits.Single().Indexes.Single().PhysicalizationIntent);
    }

    [Fact]
    public void CanCreateNativePhysicalizedStorageUnitAndOptimizedIndex()
    {
        var unit = new StorageUnitDescriptor(
            "Tenants",
            [_id, new StorageFieldDescriptor("Name", StorageFieldType.String, true)],
            [new StorageKeyDescriptor("PK_Tenants", ["Id"])],
            [new StorageIndexDescriptor("UX_Tenants_Name", [new StorageIndexFieldDescriptor("Name")], true, PhysicalizationIntent.OptimizedIndexes)],
            PhysicalizationIntent.NativePhysicalized);

        Assert.Equal(PhysicalizationIntent.NativePhysicalized, unit.PhysicalizationIntent);
        Assert.True(unit.Indexes.Single().IsUnique);
        Assert.Equal(PhysicalizationIntent.OptimizedIndexes, unit.Indexes.Single().PhysicalizationIntent);
    }

    [Fact]
    public void ManifestRequiresUniqueStorageUnitNames()
    {
        var unit = CreateUnit("Secrets");

        var exception = Assert.Throws<ArgumentException>(() => new StorageManifestDescriptor("sample.secrets", new StorageManifestVersion(1), [unit, unit]));

        Assert.Equal("storageUnits", exception.ParamName);
    }

    [Fact]
    public void StorageUnitRequiresUniqueFieldNames()
    {
        var exception = Assert.Throws<ArgumentException>(() => new StorageUnitDescriptor("Secrets", [_id, _id]));

        Assert.Equal("fields", exception.ParamName);
    }

    [Fact]
    public void KeyMustReferenceDeclaredFields()
    {
        var exception = Assert.Throws<ArgumentException>(() => new StorageUnitDescriptor(
            "Secrets",
            [_id],
            [new StorageKeyDescriptor("PK_Secrets", ["Missing"])]));

        Assert.Equal("Keys", exception.ParamName);
    }

    [Fact]
    public void IndexMustReferenceDeclaredFields()
    {
        var exception = Assert.Throws<ArgumentException>(() => new StorageUnitDescriptor(
            "Secrets",
            [_id],
            indexes: [new StorageIndexDescriptor("IX_Secrets_Missing", [new StorageIndexFieldDescriptor("Missing")])]));

        Assert.Equal("Indexes", exception.ParamName);
    }

    [Fact]
    public void IndexRequiresAtLeastOneField()
    {
        var exception = Assert.Throws<ArgumentException>(() => new StorageIndexDescriptor("IX_Secrets_Empty", []));

        Assert.Equal("fields", exception.ParamName);
    }

    [Fact]
    public void FieldRequiresName()
    {
        var exception = Assert.Throws<ArgumentException>(() => new StorageFieldDescriptor(" ", StorageFieldType.String));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void VersionRequiresPositiveMajorVersion()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new StorageManifestVersion(0));

        Assert.Equal("major", exception.ParamName);
    }

    [Fact]
    public void DescriptorsRejectUnknownEnumValues()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new StorageFieldDescriptor("Id", (StorageFieldType)999));

        Assert.Equal("type", exception.ParamName);
    }

    private StorageUnitDescriptor CreateUnit(string name) => new(
        name,
        [_id, _data],
        [new StorageKeyDescriptor($"PK_{name}", ["Id"])]);
}
