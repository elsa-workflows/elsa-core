namespace Elsa.Persistence.VNext.Physicalization;

public record PhysicalizationPlan(
    string ProviderName,
    StoragePhysicalizationPolicy Policy,
    IReadOnlyList<PhysicalizationOperation> Operations);

public record PhysicalizationOperation(
    string Name,
    string Description,
    string? CommandText = null);
