using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.AI.Persistence.EFCore.Entities;

namespace Elsa.AI.Persistence.EFCore.Stores;

public class EFCoreAiAuditSink(AiDbContext dbContext) : IAiAuditSink, IAiAuditEventHandler
{
    public async ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditRecords.Add(new AiAuditRecord
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
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
