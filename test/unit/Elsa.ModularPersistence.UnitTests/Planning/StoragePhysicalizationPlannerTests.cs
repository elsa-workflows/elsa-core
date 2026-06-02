using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Planning;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.UnitTests.Planning;

public class StoragePhysicalizationPlannerTests
{
    [Fact]
    public void PlanMarksPortableDocumentManifestAsSupportedByDefault()
    {
        var planner = new StoragePhysicalizationPlanner("Portable", ProviderCapabilities.PortableDocument);

        var plan = planner.Plan(CreateManifest());

        Assert.True(plan.IsSupported);
        Assert.All(plan.Items, x => Assert.Equal(PhysicalizationPlanStatus.Planned, x.Status));
        Assert.Contains(plan.Items, x => x.Kind == PhysicalizationPlanItemKind.StorageUnit && x.Intent == PhysicalizationIntent.PortableDocument);
        Assert.Contains(plan.Items, x => x.Kind == PhysicalizationPlanItemKind.Index && x.Intent == PhysicalizationIntent.PortableDocument);
    }

    [Fact]
    public void PlanReportsUnsupportedOptimizedIndexWhenProviderHasPortableCapabilities()
    {
        var planner = new StoragePhysicalizationPlanner("Portable", ProviderCapabilities.PortableDocument);

        var plan = planner.Plan(CreateManifest(indexIntent: PhysicalizationIntent.OptimizedIndexes));

        Assert.False(plan.IsSupported);
        var unsupported = Assert.Single(plan.Items, x => x.Status == PhysicalizationPlanStatus.Unsupported);
        Assert.Equal(PhysicalizationPlanItemKind.Index, unsupported.Kind);
        Assert.Equal("IX_Customers_Email", unsupported.Name);
        Assert.Contains("OptimizedIndexes", unsupported.Message);
    }

    [Fact]
    public void PlanAllowsOptimizedIndexWhenProviderCapabilityIsEnabled()
    {
        var capabilities = new ProviderCapabilities(
            [StorageUnitKind.Document],
            Enum.GetValues<StorageFieldType>(),
            [PhysicalizationIntent.PortableDocument, PhysicalizationIntent.OptimizedIndexes]);
        var planner = new StoragePhysicalizationPlanner("Optimized", capabilities);

        var plan = planner.Plan(CreateManifest(indexIntent: PhysicalizationIntent.OptimizedIndexes));

        Assert.True(plan.IsSupported);
        var index = Assert.Single(plan.Items, x => x.Kind == PhysicalizationPlanItemKind.Index);
        Assert.Equal(PhysicalizationIntent.OptimizedIndexes, index.Intent);
        Assert.Equal(PhysicalizationPlanStatus.Planned, index.Status);
    }

    [Fact]
    public void PlanReportsNativePhysicalizationAsUnsupportedWhenProviderDoesNotAdvertiseIt()
    {
        var planner = new StoragePhysicalizationPlanner("Portable", ProviderCapabilities.PortableDocument);

        var plan = planner.Plan(CreateManifest(unitIntent: PhysicalizationIntent.NativePhysicalized));

        Assert.False(plan.IsSupported);
        var unsupported = Assert.Single(plan.Items, x => x.Status == PhysicalizationPlanStatus.Unsupported);
        Assert.Equal(PhysicalizationPlanItemKind.StorageUnit, unsupported.Kind);
        Assert.Contains("NativePhysicalized", unsupported.Message);
    }

    private static StorageManifestDescriptor CreateManifest(
        PhysicalizationIntent unitIntent = PhysicalizationIntent.PortableDocument,
        PhysicalizationIntent indexIntent = PhysicalizationIntent.PortableDocument) =>
        new(
            "runtime.customers",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Customers",
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Email", StorageFieldType.String, true)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Customers", ["Id"])
                    ],
                    [
                        new StorageIndexDescriptor("IX_Customers_Email", [new StorageIndexFieldDescriptor("Email")], physicalizationIntent: indexIntent)
                    ],
                    unitIntent)
            ]);
}
