using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Context;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Streaming;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Services;

public class AiOrchestrator(
    IEnumerable<IAiProvider> providers,
    IAiToolRegistry toolRegistry,
    AiContextResolver contextResolver,
    AiStreamEventMapper streamEventMapper,
    IOptions<AiHostOptions> options) : IAiOrchestrator
{
    public async IAsyncEnumerable<AiStreamEvent> ExecuteChatAsync(AiChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString("N");
        var sequence = 0L;

        yield return CreateEvent("conversation.started", conversationId, sequence++);

        var context = await contextResolver.ResolveAsync(request, cancellationToken);
        var tools = await toolRegistry.ListAsync(new AiToolQuery { Agent = request.Agent, ActorId = request.UserId, TenantId = request.TenantId }, cancellationToken);
        var provider = SelectProvider(request);

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
            {
                sequence = Math.Max(sequence, providerEvent.Sequence + 1);
                yield return streamEventMapper.Map(conversationId, providerEvent);
            }
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

    private IAiProvider? SelectProvider(AiChatRequest request)
    {
        var availableProviders = providers.ToList();
        var providerName = request.ProviderName ?? FindAgentProviderName(request.Agent) ?? options.Value.DefaultProviderName;

        if (!string.IsNullOrWhiteSpace(providerName))
            return availableProviders.FirstOrDefault(x => string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase));

        return availableProviders.Count == 1 ? availableProviders[0] : null;
    }

    private string? FindAgentProviderName(string? agent) =>
        string.IsNullOrWhiteSpace(agent)
            ? null
            : options.Value.Agents.FirstOrDefault(x => string.Equals(x.Name, agent, StringComparison.OrdinalIgnoreCase))?.ProviderName;
}
