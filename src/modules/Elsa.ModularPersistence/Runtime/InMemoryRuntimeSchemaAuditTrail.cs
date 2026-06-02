using System.Collections.Concurrent;

namespace Elsa.ModularPersistence.Runtime;

public sealed class InMemoryRuntimeSchemaAuditTrail : IRuntimeSchemaAuditTrail
{
    private readonly ConcurrentQueue<RuntimeSchemaAuditEntry> _entries = new();

    public ValueTask AddAsync(RuntimeSchemaAuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries.Enqueue(entry);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyCollection<RuntimeSchemaAuditEntry>> ListAsync(string? definitionId = null, CancellationToken cancellationToken = default)
    {
        var entries = _entries
            .Where(x => string.IsNullOrWhiteSpace(definitionId) || string.Equals(x.DefinitionId, definitionId, StringComparison.Ordinal))
            .OrderBy(x => x.Timestamp)
            .ToArray();
        return ValueTask.FromResult<IReadOnlyCollection<RuntimeSchemaAuditEntry>>(entries);
    }
}
