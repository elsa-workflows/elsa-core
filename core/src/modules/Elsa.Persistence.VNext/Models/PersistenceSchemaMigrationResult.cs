namespace Elsa.Persistence.VNext;

public record PersistenceSchemaMigrationResult(
    string SchemaName,
    int FromVersion,
    int ToVersion,
    bool Applied,
    IReadOnlyList<string> Statements);
