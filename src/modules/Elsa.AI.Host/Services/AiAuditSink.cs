using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Services;

public class AiAuditSink(IEnumerable<IAiAuditEventHandler> handlers) : IAiAuditSink
{
    public async ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        foreach (var handler in handlers)
            await handler.RecordAsync(auditEvent, cancellationToken);
    }
}

public interface IAiAuditEventHandler
{
    ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
