using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Validation;
using Elsa.Secrets.Storage;

namespace Elsa.Secrets.UnitTests;

public class SecretsStorageManifestTests
{
    [Fact]
    public void ManifestCapturesSecretDocumentShapeAndIndexes()
    {
        var manifest = SecretsStorageManifest.Create();
        var unit = Assert.Single(manifest.StorageUnits);

        Assert.Equal("elsa.secrets", manifest.SchemaName);
        Assert.Equal("1.0.0", manifest.Version.ToString());
        Assert.Equal("Secrets", unit.Name);
        Assert.Equal(StorageUnitKind.Document, unit.Kind);
        Assert.Contains(unit.Fields, x => x.Name == "Name" && x.Type == StorageFieldType.String && x.IsRequired);
        Assert.Contains(unit.Fields, x => x.Name == "Status" && x.Type == StorageFieldType.String && x.IsRequired);
        Assert.Contains(unit.Fields, x => x.Name == "Versions" && x.Type == StorageFieldType.Json && x.IsRequired);
        Assert.Contains(unit.Keys, x => x.Name == "PK_Secrets" && x.Kind == StorageKeyKind.Primary && x.FieldNames.SequenceEqual(["Id"]));
        Assert.Contains(unit.Keys, x => x.Name == "UK_Secrets_Name" && x.Kind == StorageKeyKind.Unique && x.FieldNames.SequenceEqual(["Name"]));
        Assert.Contains(unit.Indexes, x => x.Name == "UX_Secrets_Name" && x.IsUnique);
        Assert.Contains(unit.Indexes, x => x.Name == "IX_Secrets_Status_Name");
        Assert.Contains(unit.Indexes, x => x.Name == "IX_Secrets_TypeName_Name");
        Assert.Contains(unit.Indexes, x => x.Name == "IX_Secrets_StoreName_Name");
        Assert.Contains(unit.Indexes, x => x.Name == "IX_Secrets_Scope_Name");
    }

    [Fact]
    public void ManifestIsPortableDocumentProviderCompatible()
    {
        var result = new StorageManifestValidator().Validate(SecretsStorageManifest.Create(), ProviderCapabilities.PortableDocument);

        Assert.True(result.IsValid);
    }
}
