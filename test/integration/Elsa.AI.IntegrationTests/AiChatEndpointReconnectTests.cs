using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.Common.Multitenancy;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Streaming;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ChatEndpoint = Elsa.AI.Host.Endpoints.Ai.Chat.Endpoint;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.AI.IntegrationTests;

public class AiChatEndpointReconnectTests
{
    [Fact(DisplayName = "Chat endpoint marks the actual reconnect conversation as connected")]
    public async Task ChatEndpointMarksTheActualReconnectConversationAsConnected()
    {
        var sessionManager = new AiStreamSessionManager();
        sessionManager.MarkDisconnected("foreign-conversation", TimeSpan.FromMinutes(5));
        sessionManager.MarkDisconnected("actual-conversation", TimeSpan.FromMinutes(5));
        var endpoint = new ChatEndpoint(
            new ReassignedConversationOrchestrator(),
            sessionManager,
            MicrosoftOptions.Create(new AiHostOptions()));
        SetHttpContext(endpoint, new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        });

        await endpoint.HandleAsync(new AiChatRequest
        {
            ConversationId = "foreign-conversation",
            UserId = "user-1",
            Message = "Reconnect"
        }, CancellationToken.None);

        Assert.True(sessionManager.CanReconnect("foreign-conversation"));
        Assert.False(sessionManager.CanReconnect("actual-conversation"));
    }

    [Fact(DisplayName = "Chat endpoint resolves tenant from tenant accessor")]
    public async Task ChatEndpointResolvesTenantFromTenantAccessor()
    {
        var tenantAccessor = new DefaultTenantAccessor();
        using var tenantScope = tenantAccessor.PushContext(new Tenant { Id = "tenant-1", Name = "Tenant 1" });
        var services = new ServiceCollection();
        services.AddSingleton<ITenantAccessor>(tenantAccessor);
        await using var provider = services.BuildServiceProvider();
        var orchestrator = new CapturingRequestOrchestrator();
        var endpoint = new ChatEndpoint(
            orchestrator,
            new AiStreamSessionManager(),
            MicrosoftOptions.Create(new AiHostOptions()));
        SetHttpContext(endpoint, new DefaultHttpContext
        {
            RequestServices = provider,
            Response =
            {
                Body = new MemoryStream()
            }
        });

        await endpoint.HandleAsync(new AiChatRequest
        {
            ConversationId = "conversation-1",
            UserId = "user-1",
            Message = "Reconnect"
        }, CancellationToken.None);

        Assert.Equal("tenant-1", orchestrator.Request!.TenantId);
    }

    private static void SetHttpContext(ChatEndpoint endpoint, HttpContext httpContext)
    {
        var property = typeof(ChatEndpoint)
            .GetProperty(nameof(ChatEndpoint.HttpContext), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        property!.SetValue(endpoint, httpContext);
    }

    private class ReassignedConversationOrchestrator : IAiOrchestrator
    {
        public async IAsyncEnumerable<AiStreamEvent> ExecuteChatAsync(AiChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            yield return new AiStreamEvent
            {
                Type = "conversation.started",
                ConversationId = "actual-conversation",
                Sequence = 0,
                Timestamp = DateTimeOffset.UtcNow
            };

            yield return new AiStreamEvent
            {
                Type = "conversation.completed",
                ConversationId = "actual-conversation",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    private class CapturingRequestOrchestrator : IAiOrchestrator
    {
        public AiChatRequest? Request { get; private set; }

        public async IAsyncEnumerable<AiStreamEvent> ExecuteChatAsync(AiChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Request = request;
            await Task.Yield();

            yield return new AiStreamEvent
            {
                Type = "conversation.completed",
                ConversationId = request.ConversationId!,
                Sequence = 0,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
