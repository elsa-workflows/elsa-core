namespace Elsa.ModularPersistence.Runtime;

public interface IRuntimeSchemaAuditTrail
{
    ValueTask AddAsync(RuntimeSchemaAuditEntry entry, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<RuntimeSchemaAuditEntry>> ListAsync(string? definitionId = null, CancellationToken cancellationToken = default);
}
