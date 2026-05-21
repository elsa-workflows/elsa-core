using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.IntegrationTests;

public class AiChatEndpointTests
{
    [Fact(DisplayName = "Chat orchestration emits conversation and assistant events")]
    public async Task ChatOrchestrationEmitsConversationAndAssistantEvents()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
            events.Add(streamEvent);

        Assert.Contains(events, x => x.Type == "conversation.started");
        Assert.Contains(events, x => x.Type == "assistant.delta");
        Assert.Contains(events, x => x.Type == "conversation.completed");
    }
}
