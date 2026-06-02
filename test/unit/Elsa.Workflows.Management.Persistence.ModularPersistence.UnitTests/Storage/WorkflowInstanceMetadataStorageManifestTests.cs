using Elsa.Workflows.Management.Persistence.ModularPersistence.Storage;

namespace Elsa.Workflows.Management.Persistence.ModularPersistence.UnitTests.Storage;

public class WorkflowInstanceMetadataStorageManifestTests
{
    [Fact]
    public void CreateDeclaresMetadataFieldsAndIndexesOnly()
    {
        var manifest = WorkflowInstanceMetadataStorageManifest.Create();
        var unit = Assert.Single(manifest.StorageUnits);
        var fieldNames = unit.Fields.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        var indexNames = unit.Indexes.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(WorkflowInstanceMetadataStorageManifest.SchemaName, manifest.SchemaName);
        Assert.Contains("Id", fieldNames);
        Assert.Contains("DefinitionId", fieldNames);
        Assert.Contains("Status", fieldNames);
        Assert.Contains("UpdatedAt", fieldNames);
        Assert.DoesNotContain("WorkflowState", fieldNames);
        Assert.Contains(WorkflowInstanceMetadataStorageManifest.StatusSubStatusDefinitionVersionIndexName, indexNames);
        Assert.Contains(WorkflowInstanceMetadataStorageManifest.UpdatedAtIndexName, indexNames);
    }
}
