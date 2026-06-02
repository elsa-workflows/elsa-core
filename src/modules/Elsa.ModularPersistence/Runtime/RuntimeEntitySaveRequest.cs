namespace Elsa.ModularPersistence.Runtime;

public sealed record RuntimeEntitySaveRequest(
    string Id,
    string Data,
    string? TenantId = null,
    long? ExpectedVersion = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
