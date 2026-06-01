using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.PostgreSql.UnitTests;

public class PostgreSqlDocumentProviderCapabilitiesTests
{
    [Fact]
    public void CreateDefaultsToPortableDocumentCapabilities()
    {
        var capabilities = PostgreSqlDocumentProviderCapabilities.Create();

        Assert.Contains(StorageUnitKind.Document, capabilities.StorageUnitKinds);
        Assert.Contains(PhysicalizationIntent.PortableDocument, capabilities.PhysicalizationIntents);
        Assert.DoesNotContain(PhysicalizationIntent.OptimizedIndexes, capabilities.PhysicalizationIntents);
    }

    [Fact]
    public void CreateIncludesOptimizedIndexesWhenEnabled()
    {
        var capabilities = PostgreSqlDocumentProviderCapabilities.Create(useOptimizedJsonbIndexes: true);

        Assert.Contains(PhysicalizationIntent.PortableDocument, capabilities.PhysicalizationIntents);
        Assert.Contains(PhysicalizationIntent.OptimizedIndexes, capabilities.PhysicalizationIntents);
    }
}
