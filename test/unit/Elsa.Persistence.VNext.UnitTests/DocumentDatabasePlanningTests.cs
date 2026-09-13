using Elsa.Persistence.VNext.Document;
using Elsa.Secrets.Persistence.VNext;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class DocumentDatabasePlanningTests
{
    private readonly SecretPersistenceSchemaProvider _schemaProvider = new();
    private readonly DocumentDatabasePlanner _planner = new();

    [Test]
    public async Task DocumentPlanner_ProducesCollectionPlanFromSecretsIntent()
    {
        var plan = _planner.Plan(_schemaProvider.DescribeSchema());
        var collection = await Assert.That(plan.Collections).HasSingleItem();

        await Assert.That(collection.Name).IsEqualTo("Secrets");
        await Assert.That(collection.Namespace).IsEqualTo("Elsa");
        await Assert.That(collection.KeyFields).IsEquivalentTo(["Id"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(collection.Fields.Count).IsEqualTo(12);
        await Assert.That(collection.Fields).Contains(x => x.Name == "Versions" && x.Type == PersistenceColumnType.Json && !x.IsNullable);
        await Assert.That(collection.Indexes).Contains(x => x.Name == "IX_Secret_Name" && x.IsUnique && x.Fields.SequenceEqual(["Name"]));
        await Assert.That(collection.Indexes).Contains(x => x.Name == "IX_Secret_Status" && x.Fields.SequenceEqual(["Status"]));
    }
}
