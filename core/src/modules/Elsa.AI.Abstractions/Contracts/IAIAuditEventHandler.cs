using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.Contracts;

public interface IAIAuditEventHandler
{
    ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default);

    async ValueTask RecordManyAsync(IReadOnlyCollection<AIAuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        foreach (var auditEvent in auditEvents)
            await RecordAsync(auditEvent, cancellationToken);
    }
}
