using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.SqlServer.UnitTests;

public class SqlServerDocumentProviderCapabilitiesTests
{
    [Fact]
    public void CreateDefaultsToPortableDocumentCapabilities()
    {
        var capabilities = SqlServerDocumentProviderCapabilities.Create();

        Assert.Contains(StorageUnitKind.Document, capabilities.StorageUnitKinds);
        Assert.Contains(PhysicalizationIntent.PortableDocument, capabilities.PhysicalizationIntents);
        Assert.DoesNotContain(PhysicalizationIntent.OptimizedIndexes, capabilities.PhysicalizationIntents);
    }

    [Fact]
    public void CreateIncludesOptimizedIndexesWhenEnabled()
    {
        var capabilities = SqlServerDocumentProviderCapabilities.Create(useOptimizedIndexes: true);

        Assert.Contains(PhysicalizationIntent.PortableDocument, capabilities.PhysicalizationIntents);
        Assert.Contains(PhysicalizationIntent.OptimizedIndexes, capabilities.PhysicalizationIntents);
    }
}
