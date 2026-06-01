using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Planning;

public sealed record PhysicalizationPlanItem(
    PhysicalizationPlanItemKind Kind,
    string Path,
    string Name,
    PhysicalizationIntent Intent,
    PhysicalizationPlanStatus Status,
    string Message);
