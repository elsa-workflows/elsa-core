using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.Contracts;

public interface IAIConversationStore
{
    ValueTask<AIConversation?> FindAsync(string id, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(AIConversation conversation, CancellationToken cancellationToken = default);
}

public interface IAITransientConversationStore : IAIConversationStore;

public interface IAIProposalStore
{
    ValueTask<AIProposal?> FindAsync(string id, string? tenantId, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(AIProposal proposal, CancellationToken cancellationToken = default);
}

public interface IAIAuditSink
{
    ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default);

    async ValueTask RecordManyAsync(IReadOnlyCollection<AIAuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        foreach (var auditEvent in auditEvents)
            await RecordAsync(auditEvent, cancellationToken);
    }
}
