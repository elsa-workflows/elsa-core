using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;

namespace Elsa.AI.Persistence.EFCore.Stores;

public class EFCoreAiAuditSink(AiDbContext dbContext) : IAiAuditEventHandler
{
    public async ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        await RecordManyAsync([auditEvent], cancellationToken);
    }

    public async ValueTask RecordManyAsync(IReadOnlyCollection<AiAuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        if (auditEvents.Count == 0)
            return;

        dbContext.AuditRecords.AddRange(auditEvents.Select(ToRecord));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AiAuditRecord ToRecord(AiAuditEvent auditEvent) =>
        new()
        {
            Id = auditEvent.Id,
            TenantId = auditEvent.TenantId,
            ActorId = auditEvent.ActorId,
            ConversationId = auditEvent.ConversationId,
            ProposalId = auditEvent.ProposalId,
            ToolInvocationId = auditEvent.ToolInvocationId,
            Type = auditEvent.Type,
            Timestamp = auditEvent.Timestamp,
            TraceId = auditEvent.TraceId,
            Summary = auditEvent.Summary,
            Data = auditEvent.Data.ToJsonString()
        };
}
