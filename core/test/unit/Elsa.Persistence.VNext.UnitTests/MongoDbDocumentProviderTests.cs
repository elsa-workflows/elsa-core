using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.MongoDb;
using MongoDB.Driver;
using NSubstitute;

namespace Elsa.Persistence.VNext.UnitTests;

public class MongoDbDocumentProviderTests
{
    [Fact]
    public void MongoDbPlanner_ProducesNativeCollectionAndIndexPlan()
    {
        var plan = new MongoDbDatabasePlanner().Plan(CreateSchema());

        var collection = Assert.Single(plan.Collections);
        Assert.Equal("Elsa_Orders", collection.CollectionName);
        Assert.Equal("Orders", collection.Collection.Name);
        Assert.Contains(collection.Indexes, x => x.Name == "IX_Orders_Status" && x.Fields.SequenceEqual(["IndexValues.Status"]));
        Assert.Contains(collection.Indexes, x => x.Name == "IX_Orders_CustomerId_Status" && x.Fields.SequenceEqual(["IndexValues.CustomerId", "IndexValues.Status"]));
    }

    [Fact]
    public void MongoDbDocumentStore_UsesProviderNeutralStoreContract()
    {
        var database = Substitute.For<IMongoDatabase>();

        var store = new MongoDbDocumentStore(database, CreateSchema());

        Assert.IsAssignableFrom<IDocumentStore>(store);
    }

    [Fact]
    public void MongoDbProvider_RejectsUndeclaredIndexShape()
    {
        var plan = new MongoDbDatabasePlanner().Plan(CreateSchema());
        var collection = Assert.Single(plan.Collections);
        var query = new DocumentQuery("Orders", new Dictionary<string, string?> { ["Priority"] = "High" });

        Assert.Throws<DocumentQueryNotIndexedException>(() => DocumentIndexMatcher.FindMatchingIndex(collection.Collection, query));
    }

    private static PersistenceSchema CreateSchema()
    {
        return new PersistenceSchemaBuilder("Orders")
            .StorageUnit("Orders", storage => storage
                .RequiredField("Id", PersistenceColumnType.String, 450)
                .RequiredField("Status", PersistenceColumnType.String, 50)
                .RequiredField("CustomerId", PersistenceColumnType.String, 450)
                .Key("PK_Orders", "Id")
                .Index("IX_Orders_Status", "Status")
                .Index("IX_Orders_CustomerId_Status", ["CustomerId", "Status"]),
                @namespace: "Elsa")
            .Build();
    }
}
