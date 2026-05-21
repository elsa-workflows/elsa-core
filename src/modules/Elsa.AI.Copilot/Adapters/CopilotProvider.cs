using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Copilot.Options;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Copilot.Adapters;

public class CopilotProvider(IOptions<CopilotOptions> options) : IAiProvider
{
    public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new AiSessionHandle
        {
            Id = request.ConversationId,
            ProviderSessionId = $"{options.Value.ProviderName}:{request.ConversationId}"
        });
    }

    public async IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        yield return new AiProviderEvent
        {
            Type = "assistant.delta",
            Sequence = 1,
            Timestamp = DateTimeOffset.UtcNow,
            Data = new JsonObject
            {
                ["content"] = "Copilot adapter is registered. Runtime CLI integration is deferred to the provider implementation slice."
            }
        };
    }
}
