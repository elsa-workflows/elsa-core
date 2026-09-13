using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.MongoDb.Physicalization;
using Elsa.Persistence.VNext.Physicalization;
using Elsa.Persistence.VNext.Sqlite.Physicalization;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class PhysicalizationPlanningTests
{
    private readonly PersistenceSchema _schema = new PersistenceSchemaBuilder("RuntimeEntities")
        .StorageUnit("RuntimeEntityInstances", storage => storage
                .RequiredField("Id", PersistenceColumnType.String, 450)
                .RequiredField("Status", PersistenceColumnType.String, 50)
                .RequiredField("Number", PersistenceColumnType.String, 100)
                .Key("PK_RuntimeEntityInstances", "Id")
                .Index("IX_RuntimeEntityInstances_Status", "Status")
                .Index("UX_RuntimeEntityInstances_Number", ["Number"], true),
            @namespace: "Elsa")
        .Build();
    private readonly StoragePhysicalizationPolicy _policy = new(
        "RuntimeEntityInstances",
        PhysicalizationTarget.DedicatedRelationalTable,
        "RuntimeEntity_Order",
        [
            new("IX_RuntimeEntity_Order_Status", ["Status"]),
            new("UX_RuntimeEntity_Order_Number", ["Number"], true)
        ]);

    [Test]
    public async Task SqlitePlanner_ProducesDedicatedTableAndIndexOperations()
    {
        var plan = new SqlitePhysicalizationPlanner().Plan(_schema, _policy);

        await Assert.That(plan.ProviderName).IsEqualTo("SQLite");
        await Assert.That(plan.Policy).IsEqualTo(_policy);
        await Assert.That(plan.Operations.Count).IsEqualTo(3);
        await Assert.That(plan.Operations).Contains(x => x.Name == "CreateTable" && x.CommandText!.Contains("CREATE TABLE IF NOT EXISTS \"RuntimeEntity_Order\""));
        await Assert.That(plan.Operations).Contains(x => x.Name == "CreateTable" && x.CommandText!.Contains("\"Status\" TEXT NULL"));
        await Assert.That(plan.Operations).Contains(x => x.Name == "CreateTable" && x.CommandText!.Contains("\"Number\" TEXT NULL"));
        await Assert.That(plan.Operations).Contains(x => x.Name == "CreateIndex" && x.CommandText == "CREATE INDEX IF NOT EXISTS \"IX_RuntimeEntity_Order_Status\" ON \"RuntimeEntity_Order\" (\"Status\");");
        await Assert.That(plan.Operations).Contains(x => x.Name == "CreateIndex" && x.CommandText == "CREATE UNIQUE INDEX IF NOT EXISTS \"UX_RuntimeEntity_Order_Number\" ON \"RuntimeEntity_Order\" (\"Number\");");
    }

    [Test]
    public void SqlitePlanner_RejectsDocumentCollectionTarget()
    {
        var policy = _policy with { Target = PhysicalizationTarget.DedicatedDocumentCollection };

        Assert.ThrowsExactly<InvalidOperationException>(() => new SqlitePhysicalizationPlanner().Plan(_schema, policy));
    }

    [Test]
    public async Task MongoDbPlanner_ProducesDedicatedCollectionAndIndexOperations()
    {
        var policy = _policy with { Target = PhysicalizationTarget.DedicatedDocumentCollection };

        var plan = new MongoDbPhysicalizationPlanner().Plan(_schema, policy);

        await Assert.That(plan.ProviderName).IsEqualTo("MongoDB");
        await Assert.That(plan.Policy).IsEqualTo(policy);
        await Assert.That(plan.Operations.Count).IsEqualTo(3);
        await Assert.That(plan.Operations).Contains(x => x.Name == "CreateCollection" && x.Description.Contains("RuntimeEntity_Order"));
        await Assert.That(plan.Operations).Contains(x => x.Name == "CreateIndex" && x.Description.Contains("Data.Status:1"));
        await Assert.That(plan.Operations).Contains(x => x.Name == "CreateIndex" && x.Description.Contains("unique MongoDB index UX_RuntimeEntity_Order_Number"));
    }

    [Test]
    public void MongoDbPlanner_RejectsRelationalTableTarget()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => new MongoDbPhysicalizationPlanner().Plan(_schema, _policy));
    }

    [Test]
    public void Planner_RejectsPolicyForUndeclaredField()
    {
        var policy = _policy with { Indexes = [new("IX_Missing", ["Missing"])] };

        Assert.ThrowsExactly<InvalidOperationException>(() => new SqlitePhysicalizationPlanner().Plan(_schema, policy));
    }
}
