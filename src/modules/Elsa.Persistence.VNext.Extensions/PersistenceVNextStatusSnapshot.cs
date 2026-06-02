namespace Elsa.Persistence.VNext.Extensions;

public record PersistenceVNextStatusSnapshot(
    bool MaterializationEnabled,
    bool Succeeded,
    DateTimeOffset? LastAttemptedAt,
    DateTimeOffset? LastSucceededAt,
    IReadOnlyList<string> SchemaNames,
    IReadOnlyList<string> StorageUnits,
    IReadOnlyList<string> DocumentStoreTypes,
    string? ErrorMessage = null)
{
    public static PersistenceVNextStatusSnapshot NotStarted { get; } = new(
        MaterializationEnabled: false,
        Succeeded: false,
        LastAttemptedAt: null,
        LastSucceededAt: null,
        SchemaNames: [],
        StorageUnits: [],
        DocumentStoreTypes: []);
}
