using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.Contracts;

public interface IAiAuditEventHandler
{
    ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
