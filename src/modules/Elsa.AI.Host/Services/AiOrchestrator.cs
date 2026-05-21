using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Context;
using Elsa.AI.Host.Streaming;

namespace Elsa.AI.Host.Services;

public class AiOrchestrator(
    IEnumerable<IAiProvider> providers,
    IAiToolRegistry toolRegistry,
    AiContextResolver contextResolver,
    AiStreamEventMapper streamEventMapper) : IAiOrchestrator
{
    public async IAsyncEnumerable<AiStreamEvent> ExecuteChatAsync(AiChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString("N");
        var sequence = 0L;

        yield return CreateEvent("conversation.started", conversationId, sequence++);

        var context = await contextResolver.ResolveAsync(request, cancellationToken);
        var tools = await toolRegistry.ListAsync(new AiToolQuery { Agent = request.Agent, ActorId = request.UserId, TenantId = request.TenantId }, cancellationToken);
        var provider = providers.FirstOrDefault();

        if (provider == null)
        {
            yield return CreateEvent("assistant.delta", conversationId, sequence++, new JsonObject
            {
                ["content"] = "Weaver is ready, but no AI provider is configured."
            });
        }
        else
        {
            await foreach (var providerEvent in provider.ExecuteTurnAsync(new AiTurnRequest
                           {
                               ConversationId = conversationId,
                               Message = request.Message,
                               Context = context,
                               Tools = tools,
                               Agent = request.Agent
                           }, cancellationToken))
                yield return streamEventMapper.Map(conversationId, providerEvent);
        }

        yield return CreateEvent("conversation.completed", conversationId, sequence);
    }

    private static AiStreamEvent CreateEvent(string type, string conversationId, long sequence, JsonObject? data = null) =>
        new()
        {
            Type = type,
            ConversationId = conversationId,
            Sequence = sequence,
            Timestamp = DateTimeOffset.UtcNow,
            Data = data ?? []
        };
}
