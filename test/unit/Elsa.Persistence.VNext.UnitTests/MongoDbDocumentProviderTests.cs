using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.MongoDb;
using MongoDB.Driver;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class MongoDbDocumentProviderTests
{
    [Test]
    public async Task MongoDbPlanner_ProducesNativeCollectionAndIndexPlan()
    {
        var plan = new MongoDbDatabasePlanner().Plan(CreateSchema());

        var collection = await Assert.That(plan.Collections).HasSingleItem();
        await Assert.That(collection.CollectionName).IsEqualTo("Elsa_Orders");
        await Assert.That(collection.Collection.Name).IsEqualTo("Orders");
        await Assert.That(collection.Indexes).Contains(x => x.Name == "IX_Orders_Status" && x.Fields.SequenceEqual(["IndexValues.Status"]));
        await Assert.That(collection.Indexes).Contains(x => x.Name == "IX_Orders_CustomerId_Status" && x.Fields.SequenceEqual(["IndexValues.CustomerId", "IndexValues.Status"]));
    }

    [Test]
    public async Task MongoDbDocumentStore_UsesProviderNeutralStoreContract()
    {
        var database = Substitute.For<IMongoDatabase>();

        var store = new MongoDbDocumentStore(database, CreateSchema());

        await Assert.That(store).IsAssignableTo<IDocumentStore>();
    }

    [Test]
    public async Task MongoDbProvider_RejectsUndeclaredIndexShape()
    {
        var plan = new MongoDbDatabasePlanner().Plan(CreateSchema());
        var collection = await Assert.That(plan.Collections).HasSingleItem();
        var query = new DocumentQuery("Orders", new Dictionary<string, string?> { ["Priority"] = "High" });

        Assert.ThrowsExactly<DocumentQueryNotIndexedException>(() => DocumentIndexMatcher.FindMatchingIndex(collection.Collection, query));
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
