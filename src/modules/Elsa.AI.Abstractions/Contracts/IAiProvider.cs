using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.Contracts;

public interface IAiProvider
{
    ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, CancellationToken cancellationToken = default);
}

public interface IAiOrchestrator
{
    IAsyncEnumerable<AiStreamEvent> ExecuteChatAsync(AiChatRequest request, CancellationToken cancellationToken = default);
}
