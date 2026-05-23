using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;
using Microsoft.Extensions.Logging;

namespace Elsa.AI.Persistence.EFCore.Stores;

public class EFCoreAIAuditSink(AIDbContext dbContext, ILogger<EFCoreAIAuditSink> logger) : IAIAuditEventHandler
{
    public async ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        await RecordManyAsync([auditEvent], cancellationToken);
    }

    public async ValueTask RecordManyAsync(IReadOnlyCollection<AIAuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        if (auditEvents.Count == 0)
            return;

        try
        {
            foreach (var auditEvent in auditEvents)
                dbContext.AuditRecords.Add(ToRecord(auditEvent));

            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            dbContext.ChangeTracker.Clear();
            logger.LogWarning(e, "Failed to persist {AuditEventCount} AI audit event(s)", auditEvents.Count);
        }
    }

    private static AIAuditRecord ToRecord(AIAuditEvent auditEvent) =>
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
