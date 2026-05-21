using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.Contracts;

public interface IAiConversationStore
{
    ValueTask<AiConversation?> FindAsync(string id, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(AiConversation conversation, CancellationToken cancellationToken = default);
}

public interface IAiProposalStore
{
    ValueTask<AiProposal?> FindAsync(string id, string? tenantId, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(AiProposal proposal, CancellationToken cancellationToken = default);
}

public interface IAiAuditSink
{
    ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
