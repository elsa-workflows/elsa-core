using Elsa.ModularPersistence.MongoDb.Options;
using Elsa.ModularPersistence.MongoDb.Services;

namespace Elsa.ModularPersistence.MongoDb.UnitTests;

public class MongoDbCollectionResolverTests
{
    [Fact]
    public void DefaultsToSharedCollectionStrategy()
    {
        var options = new MongoDbModularPersistenceOptions();
        var resolver = new MongoDbCollectionResolver(options);

        Assert.Equal(MongoDbCollectionStrategy.SharedCollection, options.CollectionStrategy);
        Assert.Equal("ModularPersistenceDocuments", resolver.GetCollectionName("Secrets"));
        Assert.Equal(["ModularPersistenceDocuments"], resolver.GetCollectionNames(["Secrets", "Workflows"]));
    }

    [Fact]
    public void CollectionPerTypeUsesSanitizedDocumentType()
    {
        var options = new MongoDbModularPersistenceOptions
        {
            CollectionStrategy = MongoDbCollectionStrategy.CollectionPerType,
            CollectionPerTypePrefix = "Elsa"
        };
        var resolver = new MongoDbCollectionResolver(options);

        Assert.Equal("Elsa_Workflow_Instances", resolver.GetCollectionName("Workflow-Instances"));
        Assert.Equal(["Elsa_Secrets", "Elsa_Workflows"], resolver.GetCollectionNames(["Secrets", "Workflows", "Secrets"]));
    }
}
