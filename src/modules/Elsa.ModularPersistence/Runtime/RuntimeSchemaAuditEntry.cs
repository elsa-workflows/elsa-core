namespace Elsa.ModularPersistence.Runtime;

public sealed record RuntimeSchemaAuditEntry(
    string Id,
    string DefinitionId,
    RuntimeSchemaAuditAction Action,
    string Actor,
    DateTimeOffset Timestamp,
    RuntimeStorageDefinition? Before,
    RuntimeStorageDefinition? After,
    string? ProviderName,
    bool Succeeded,
    IReadOnlyCollection<string> ProviderResults);
