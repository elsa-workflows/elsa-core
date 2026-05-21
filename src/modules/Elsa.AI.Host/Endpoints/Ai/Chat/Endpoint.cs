using Elsa.Abstractions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Permissions;
using JetBrains.Annotations;

namespace Elsa.AI.Host.Endpoints.Ai.Chat;

[PublicAPI]
public class Endpoint(IAiOrchestrator orchestrator) : ElsaEndpoint<AiChatRequest, IReadOnlyCollection<AiStreamEvent>>
{
    public override void Configure()
    {
        Post("/ai/chat");
        ConfigurePermissions(AiPermissions.Chat);
    }

    public override async Task<IReadOnlyCollection<AiStreamEvent>> ExecuteAsync(AiChatRequest request, CancellationToken cancellationToken)
    {
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(request, cancellationToken))
            events.Add(streamEvent);

        return events;
    }
}
