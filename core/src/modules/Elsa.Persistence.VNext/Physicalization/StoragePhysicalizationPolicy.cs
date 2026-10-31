namespace Elsa.Persistence.VNext.Physicalization;

public record StoragePhysicalizationPolicy(
    string StorageUnit,
    PhysicalizationTarget Target,
    string? PhysicalName = null,
    IReadOnlyList<PhysicalizedIndexPolicy>? Indexes = null)
{
    public IReadOnlyList<PhysicalizedIndexPolicy> Indexes { get; init; } = Indexes ?? [];
}
