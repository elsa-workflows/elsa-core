using Elsa.Persistence.VNext.Document;
using Elsa.Secrets.Persistence.VNext;

namespace Elsa.Persistence.VNext.UnitTests;

public class DocumentDatabasePlanningTests
{
    private readonly SecretPersistenceSchemaProvider _schemaProvider = new();
    private readonly DocumentDatabasePlanner _planner = new();

    [Fact]
    public void DocumentPlanner_ProducesCollectionPlanFromSecretsIntent()
    {
        var plan = _planner.Plan(_schemaProvider.DescribeSchema());
        var collection = Assert.Single(plan.Collections);

        Assert.Equal("Secrets", collection.Name);
        Assert.Equal("Elsa", collection.Namespace);
        Assert.Equal(["Id"], collection.KeyFields);
        Assert.Equal(12, collection.Fields.Count);
        Assert.Contains(collection.Fields, x => x.Name == "Versions" && x.Type == PersistenceColumnType.Json && !x.IsNullable);
        Assert.Contains(collection.Indexes, x => x.Name == "IX_Secret_Name" && x.IsUnique && x.Fields.SequenceEqual(["Name"]));
        Assert.Contains(collection.Indexes, x => x.Name == "IX_Secret_Status" && x.Fields.SequenceEqual(["Status"]));
    }
}
