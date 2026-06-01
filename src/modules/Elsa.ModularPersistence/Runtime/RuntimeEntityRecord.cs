namespace Elsa.ModularPersistence.Runtime;

public sealed record RuntimeEntityRecord(
    string Id,
    string? TenantId,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Data,
    IReadOnlyDictionary<string, string> Metadata);
