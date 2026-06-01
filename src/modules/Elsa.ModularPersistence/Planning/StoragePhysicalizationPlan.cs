namespace Elsa.ModularPersistence.Planning;

public sealed record StoragePhysicalizationPlan(
    string ProviderName,
    string SchemaName,
    string Version,
    IReadOnlyCollection<PhysicalizationPlanItem> Items)
{
    public bool IsSupported => Items.All(x => x.Status != PhysicalizationPlanStatus.Unsupported);
}
