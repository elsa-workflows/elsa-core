using System.Text.Json;
using Elsa.Abstractions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Permissions;
using Elsa.AI.Host.Streaming;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Endpoints.Ai.Chat;

[PublicAPI]
public class Endpoint(
    IAiOrchestrator orchestrator,
    AiStreamSessionManager sessionManager,
    IOptions<AiHostOptions> options) : ElsaEndpoint<AiChatRequest>
{
    public override void Configure()
    {
        Post("/ai/chat");
        ConfigurePermissions(AiPermissions.Chat);
    }

    public override async Task HandleAsync(AiChatRequest request, CancellationToken cancellationToken)
    {
        request = request with { ConversationId = request.ConversationId ?? Guid.NewGuid().ToString("N") };
        var response = HttpContext.Response;
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var streamEvent in orchestrator.ExecuteChatAsync(request, cancellationToken))
            {
                await response.WriteAsync($"event: {streamEvent.Type}\n", cancellationToken);
                await response.WriteAsync($"data: {JsonSerializer.Serialize(streamEvent)}\n\n", cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            sessionManager.MarkDisconnected(request.ConversationId, options.Value.ReconnectGrace);
        }
    }
}
