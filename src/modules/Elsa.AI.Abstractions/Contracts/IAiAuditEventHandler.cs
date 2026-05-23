using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.Contracts;

public interface IAiAuditEventHandler
{
    ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default);

    async ValueTask RecordManyAsync(IReadOnlyCollection<AiAuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        foreach (var auditEvent in auditEvents)
            await RecordAsync(auditEvent, cancellationToken);
    }
}
