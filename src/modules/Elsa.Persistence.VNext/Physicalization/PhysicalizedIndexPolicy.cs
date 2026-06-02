namespace Elsa.Persistence.VNext.Physicalization;

public record PhysicalizedIndexPolicy(
    string Name,
    IReadOnlyList<string> Fields,
    bool IsUnique = false);
