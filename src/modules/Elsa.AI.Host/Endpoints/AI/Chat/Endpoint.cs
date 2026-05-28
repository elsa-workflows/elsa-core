using System.Text.Json;
using Elsa.Abstractions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Endpoints.AI;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Permissions;
using Elsa.AI.Host.Streaming;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Endpoints.AI.Chat;

[PublicAPI]
public class Endpoint(
    IAIOrchestrator orchestrator,
    AIStreamSessionManager sessionManager,
    IOptions<AIHostOptions> options) : ElsaEndpoint<AIChatRequest>
{
    public override void Configure()
    {
        Post("/ai/chat");
        ConfigurePermissions(AIPermissions.Chat);
    }

    public override async Task HandleAsync(AIChatRequest request, CancellationToken cancellationToken)
    {
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString("N");
        var userPermissions = AIHttpContextIdentity.GetPermissions(HttpContext);
        request = request with
        {
            ConversationId = conversationId,
            Message = request.Message ?? "",
            Attachments = request.Attachments ?? [],
            IsReconnect = sessionManager.CanReconnect(conversationId),
            TenantId = AIHttpContextIdentity.GetTenantId(HttpContext),
            UserId = AIHttpContextIdentity.GetActorId(HttpContext),
            UserPermissions = userPermissions,
            Agent = AIHttpContextIdentity.GetAuthorizedAgent(request.Agent, options.Value, userPermissions),
            ProviderName = null
        };
        var response = HttpContext.Response;
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Connection = "keep-alive";

        var completed = false;
        var reconnectAccepted = request.IsReconnect;
        var requestedReconnectConversationId = request.ConversationId;
        var reconnectConnected = false;
        var disconnectedConversationId = request.ConversationId;
        try
        {
            await foreach (var streamEvent in orchestrator.ExecuteChatAsync(request, cancellationToken))
            {
                disconnectedConversationId = streamEvent.ConversationId;
                if (reconnectAccepted && !reconnectConnected)
                {
                    if (!string.Equals(requestedReconnectConversationId, disconnectedConversationId, StringComparison.OrdinalIgnoreCase))
                        sessionManager.ReleaseReconnect(requestedReconnectConversationId);

                    sessionManager.MarkConnected(disconnectedConversationId);
                    reconnectConnected = true;
                }

                await response.WriteAsync($"event: {streamEvent.Type}\n", cancellationToken);
                await response.WriteAsync($"data: {JsonSerializer.Serialize(streamEvent)}\n\n", cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }

            completed = true;
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            // Expected when the client disconnects; the finally block records reconnect state.
            return;
        }
        finally
        {
            if (!completed)
                sessionManager.MarkDisconnected(disconnectedConversationId, options.Value.ReconnectGrace);
        }
    }
}
