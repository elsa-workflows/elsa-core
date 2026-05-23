using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Copilot.Options;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Copilot.Adapters;

public class CopilotProvider(IOptions<CopilotOptions> options) : IAIProvider
{
    public string Name => options.Value.ProviderName ?? "copilot";

    public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default)
    {
        var providerName = request.ProviderConfiguration?.Name ?? options.Value.ProviderName ?? "copilot";

        return ValueTask.FromResult(new AISessionHandle
        {
            Id = request.ConversationId,
            ProviderSessionId = $"{providerName}:{request.ConversationId}"
        });
    }

    public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        yield return new AIProviderEvent
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
