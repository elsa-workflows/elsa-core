using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text;
using System.Text.Json.Nodes;

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

    [Fact(DisplayName = "Chat orchestration emits completion after provider sequence")]
    public async Task ChatOrchestrationEmitsCompletionAfterProviderSequence()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider, SequencedAiProvider>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
            events.Add(streamEvent);

        var completion = Assert.Single(events, x => x.Type == "conversation.completed");
        Assert.Equal(4, completion.Sequence);
    }

    [Fact(DisplayName = "Chat orchestration routes to requested provider")]
    public async Task ChatOrchestrationRoutesToRequestedProvider()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider>(new NamedAiProvider("first"));
        services.AddSingleton<IAiProvider>(new NamedAiProvider("second"));
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           ProviderName = "second",
                           Message = "Explain this workflow"
                       }))
            events.Add(streamEvent);

        var delta = Assert.Single(events, x => x.Type == "assistant.delta");
        Assert.Equal("second", delta.Data["provider"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Chat orchestration routes an agent to its configured provider")]
    public async Task ChatOrchestrationRoutesAgentToConfiguredProvider()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices(options =>
        {
            options.Agents =
            [
                new()
                {
                    Name = "workflow-author",
                    DisplayName = "Workflow author",
                    Description = "Creates safe workflow proposals",
                    ProviderName = "second"
                }
            ];
        });
        services.AddSingleton<IAiProvider>(new NamedAiProvider("first"));
        services.AddSingleton<IAiProvider>(new NamedAiProvider("second"));
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           Agent = "workflow-author",
                           Message = "Create a workflow"
                       }))
            events.Add(streamEvent);

        var delta = Assert.Single(events, x => x.Type == "assistant.delta");
        Assert.Equal("second", delta.Data["provider"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Chat orchestration ignores disabled configured providers")]
    public async Task ChatOrchestrationIgnoresDisabledConfiguredProviders()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices(options =>
        {
            options.Providers =
            [
                new()
                {
                    Name = "disabled",
                    Provider = "disabled",
                    Enabled = false
                }
            ];
        });
        services.AddSingleton<IAiProvider>(new NamedAiProvider("disabled"));
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           ProviderName = "disabled",
                           Message = "Explain this workflow"
                       }))
            events.Add(streamEvent);

        var delta = Assert.Single(events, x => x.Type == "assistant.delta");
        Assert.Equal("Weaver is ready, but no AI provider is configured.", delta.Data["content"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Chat orchestration passes selected provider configuration")]
    public async Task ChatOrchestrationPassesSelectedProviderConfiguration()
    {
        var capturingProvider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices(options =>
        {
            options.Providers =
            [
                new()
                {
                    Name = "configured",
                    Provider = capturingProvider.Name,
                    Model = "model-1",
                    ApiKeySecretName = "secret-1",
                    Endpoint = "https://example.local"
                }
            ];
        });
        services.AddSingleton<IAiProvider>(capturingProvider);
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           ProviderName = "configured",
                           Message = "Explain this workflow"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var sessionRequest = Assert.Single(capturingProvider.SessionRequests);
        var turnRequest = Assert.Single(capturingProvider.Requests);
        Assert.Equal("provider-session-" + sessionRequest.ConversationId, turnRequest.ProviderSessionId);
        Assert.Equal("configured", sessionRequest.ProviderConfiguration!.Name);
        Assert.Equal(capturingProvider.Name, turnRequest.ProviderConfiguration!.Provider);
        Assert.Equal("model-1", turnRequest.ProviderConfiguration.Model);
        Assert.Equal("secret-1", turnRequest.ProviderConfiguration.ApiKeySecretName);
        Assert.Equal("https://example.local", turnRequest.ProviderConfiguration.Endpoint);
    }

    [Fact(DisplayName = "Chat orchestration records start and completion audit events")]
    public async Task ChatOrchestrationRecordsStartAndCompletionAuditEvents()
    {
        var auditSink = new CapturingAuditSink();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.RemoveAll<IAiAuditSink>();
        services.AddSingleton<IAiAuditSink>(auditSink);
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           TenantId = "tenant-1",
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        Assert.Collection(
            auditSink.Events,
            started =>
            {
                Assert.Equal("chat.started", started.Type);
                Assert.Equal("conversation-1", started.ConversationId);
                Assert.Equal("tenant-1", started.TenantId);
                Assert.Equal("user-1", started.ActorId);
            },
            completed => Assert.Equal("chat.completed", completed.Type));
    }

    [Fact(DisplayName = "Chat orchestration continues when audit sink fails")]
    public async Task ChatOrchestrationContinuesWhenAuditSinkFails()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.RemoveAll<IAiAuditSink>();
        services.AddSingleton<IAiAuditSink, ThrowingAuditSink>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
            events.Add(streamEvent);

        Assert.Contains(events, x => x.Type == "conversation.completed");
    }

    [Fact(DisplayName = "Chat orchestration continues when conversation persistence fails")]
    public async Task ChatOrchestrationContinuesWhenConversationPersistenceFails()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.RemoveAll<IAiConversationStore>();
        services.AddSingleton<IAiConversationStore, ThrowingConversationStore>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
            events.Add(streamEvent);

        Assert.Contains(events, x => x.Type == "conversation.started");
        Assert.Contains(events, x => x.Type == "assistant.delta");
        Assert.Contains(events, x => x.Type == "conversation.completed");
    }

    [Fact(DisplayName = "Chat orchestration emits terminal events when context resolution fails")]
    public async Task ChatOrchestrationEmitsTerminalEventsWhenContextResolutionFails()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiContextProvider, ThrowingContextProvider>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var store = provider.GetRequiredService<IAiConversationStore>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Explain this workflow",
                           Attachments = [new AiContextAttachment { Kind = ThrowingContextProvider.ContextKind, ReferenceId = "workflow-1" }]
                       }))
            events.Add(streamEvent);

        var conversation = await store.FindAsync("conversation-1");

        Assert.Contains(events, x => x.Type == "conversation.started");
        Assert.Contains(events, x => x.Type == "conversation.error");
        Assert.Contains(events, x => x.Type == "conversation.completed");
        Assert.Equal(AiConversationStatus.Failed, conversation!.Status);
    }

    [Fact(DisplayName = "Chat orchestration emits terminal events when provider session creation fails")]
    public async Task ChatOrchestrationEmitsTerminalEventsWhenProviderSessionCreationFails()
    {
        var auditSink = new CapturingAuditSink();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider, ThrowingSessionProvider>();
        services.RemoveAll<IAiAuditSink>();
        services.AddSingleton<IAiAuditSink>(auditSink);
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var store = provider.GetRequiredService<IAiConversationStore>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
            events.Add(streamEvent);

        var conversation = await store.FindAsync("conversation-1");

        Assert.Contains(events, x => x.Type == "conversation.started");
        Assert.Contains(events, x => x.Type == "conversation.error");
        Assert.Contains(events, x => x.Type == "conversation.completed");
        Assert.Equal(AiConversationStatus.Failed, conversation!.Status);
        Assert.Contains(auditSink.Events, x => x.Type == "chat.failed");
    }

    [Fact(DisplayName = "Chat orchestration executes provider tool calls")]
    public async Task ChatOrchestrationExecutesProviderToolCalls()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider, ToolCallAiProvider>();
        services.AddSingleton<IAiTool, EchoTool>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
            events.Add(streamEvent);

        var toolResult = Assert.Single(events, x => x.Type == "tool.result");
        Assert.Equal("echo", toolResult.Data["toolName"]!.GetValue<string>());
        Assert.Equal(AiToolInvocationStatus.Completed.ToString(), toolResult.Data["status"]!.GetValue<string>());
        Assert.Equal("Echoed", toolResult.Data["summary"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Chat orchestration audits unresolved tool calls")]
    public async Task ChatOrchestrationAuditsUnresolvedToolCalls()
    {
        var auditSink = new CapturingAuditSink();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.RemoveAll<IAiAuditSink>();
        services.AddSingleton<IAiAuditSink>(auditSink);
        services.AddSingleton<IAiProvider, ToolCallAiProvider>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var toolAudit = Assert.Single(auditSink.Events, x => x.Type == "tool.failed");
        Assert.Equal("tool-call-1", toolAudit.ToolInvocationId);
        Assert.Equal("echo", toolAudit.Data["toolName"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Chat orchestration records tool audit timestamps around execution")]
    public async Task ChatOrchestrationRecordsToolAuditTimestampsAroundExecution()
    {
        var auditSink = new CapturingAuditSink();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.RemoveAll<IAiAuditSink>();
        services.AddSingleton<IAiAuditSink>(auditSink);
        services.AddSingleton<IAiProvider, ToolCallAiProvider>();
        services.AddSingleton<IAiTool, DelayedEchoTool>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var invoked = Assert.Single(auditSink.Events, x => x.Type == "tool.invoked");
        var completed = Assert.Single(auditSink.Events, x => x.Type == "tool.completed");

        Assert.True(invoked.Timestamp < completed.Timestamp);
    }

    [Fact(DisplayName = "Chat orchestration redacts tool exception messages from stream events")]
    public async Task ChatOrchestrationRedactsToolExceptionMessagesFromStreamEvents()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider, ToolCallAiProvider>();
        services.AddSingleton<IAiTool, ThrowingTool>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
            events.Add(streamEvent);

        var toolResult = Assert.Single(events, x => x.Type == "tool.result");
        Assert.Equal(AiToolInvocationStatus.Failed.ToString(), toolResult.Data["status"]!.GetValue<string>());
        Assert.Equal("Tool execution failed.", toolResult.Data["error"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Chat orchestration sends tool results back to provider continuations")]
    public async Task ChatOrchestrationSendsToolResultsBackToProviderContinuations()
    {
        var provider = new ToolContinuationAiProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider>(provider);
        services.AddSingleton<IAiTool, EchoTool>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
            events.Add(streamEvent);

        var continuation = Assert.Single(provider.Requests, x => x.Messages.Any(message => message.Role == AiMessageRole.Tool));
        var continuationMessages = continuation.Messages.Where(x => x.Role is AiMessageRole.Assistant or AiMessageRole.Tool).ToList();

        Assert.Empty(continuation.ToolResults);
        Assert.Collection(
            continuationMessages,
            assistant => Assert.Equal(AiMessageRole.Assistant, assistant.Role),
            tool =>
            {
                Assert.Equal(AiMessageRole.Tool, tool.Role);
                Assert.Equal("tool-call-1", tool.Metadata["toolCallId"]!.GetValue<string>());
                Assert.Equal("Echoed", tool.Content);
            });
        Assert.Contains(events, x => x.Type == "assistant.delta" && x.Data["content"]!.GetValue<string>() == "Used Echoed");
    }

    [Fact(DisplayName = "Chat orchestration persists conversation state")]
    public async Task ChatOrchestrationPersistsConversationState()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var store = provider.GetRequiredService<IAiConversationStore>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var conversation = await store.FindAsync("conversation-1");

        Assert.NotNull(conversation);
        Assert.Equal(AiConversationStatus.Completed, conversation.Status);
        Assert.NotNull(conversation.RetentionExpiresAt);
        Assert.True(conversation.Messages.Single(x => x.Role == AiMessageRole.User).StreamSequence > 0);
        Assert.Contains(conversation.Messages, x => x.Role == AiMessageRole.User && x.Content == "Explain this workflow");
    }

    [Fact(DisplayName = "Chat orchestration creates provider sessions")]
    public async Task ChatOrchestrationCreatesProviderSessions()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAiOrchestrator>();
        var store = serviceProvider.GetRequiredService<IAiConversationStore>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var sessionRequest = Assert.Single(provider.SessionRequests);
        var conversation = await store.FindAsync("conversation-1");

        Assert.Equal("conversation-1", sessionRequest.ConversationId);
        Assert.Equal("provider-session-conversation-1", conversation!.ProviderSessionId);
    }

    [Fact(DisplayName = "Chat orchestration persists generated provider session IDs")]
    public async Task ChatOrchestrationPersistsGeneratedProviderSessionIds()
    {
        var provider = new DefaultSessionHandleProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAiOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "First"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Second"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        Assert.Single(provider.SessionRequests);
    }

    [Fact(DisplayName = "Chat orchestration forwards persisted message history")]
    public async Task ChatOrchestrationForwardsPersistedMessageHistory()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAiOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "First"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Second"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var secondRequest = provider.Requests.Last();

        Assert.Equal("Second", secondRequest.Message);
        Assert.Contains(secondRequest.Messages, x => x.Role == AiMessageRole.User && x.Content == "First");
        Assert.Contains(secondRequest.Messages, x => x.Role == AiMessageRole.Assistant);
        Assert.DoesNotContain(secondRequest.Messages, x => x.Role == AiMessageRole.User && x.Content == "Second");
    }

    [Fact(DisplayName = "Chat orchestration does not duplicate reconnect user messages")]
    public async Task ChatOrchestrationDoesNotDuplicateReconnectUserMessages()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var store = provider.GetRequiredService<IAiConversationStore>();

        await store.SaveAsync(new AiConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            Status = AiConversationStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AiMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AiMessageRole.User,
                    Content = "Retry me",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Retry me\r\n",
                           IsReconnect = true
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var conversation = await store.FindAsync("conversation-1");
        var userMessages = conversation!.Messages.Where(x => x.Role == AiMessageRole.User && x.Content == "Retry me").ToList();

        Assert.Single(userMessages);
    }

    [Fact(DisplayName = "Chat orchestration continues reconnect sequences after persisted messages")]
    public async Task ChatOrchestrationContinuesReconnectSequencesAfterPersistedMessages()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var store = provider.GetRequiredService<IAiConversationStore>();

        await store.SaveAsync(new AiConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            Status = AiConversationStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AiMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AiMessageRole.User,
                    Content = "Retry me",
                    CreatedAt = DateTimeOffset.UtcNow,
                    StreamSequence = 3
                }
            ]
        });

        var events = new List<AiStreamEvent>();
        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Retry me",
                           IsReconnect = true
                       }))
            events.Add(streamEvent);

        var startedEvent = Assert.Single(events, x => x.Type == "conversation.started");
        Assert.Equal(4, startedEvent.Sequence);
    }

    [Fact(DisplayName = "Chat orchestration does not replay completed conversations on reconnect")]
    public async Task ChatOrchestrationDoesNotReplayCompletedConversationsOnReconnect()
    {
        var turnProvider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider>(turnProvider);
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var store = provider.GetRequiredService<IAiConversationStore>();
        var events = new List<AiStreamEvent>();

        await store.SaveAsync(new AiConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            Status = AiConversationStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AiMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AiMessageRole.User,
                    Content = "Retry me",
                    CreatedAt = DateTimeOffset.UtcNow,
                    StreamSequence = 0
                },
                new AiMessage
                {
                    Id = "message-2",
                    ConversationId = "conversation-1",
                    Role = AiMessageRole.Assistant,
                    Content = "Done",
                    CreatedAt = DateTimeOffset.UtcNow,
                    StreamSequence = 1
                }
            ]
        });

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Retry me",
                           IsReconnect = true
                       }))
            events.Add(streamEvent);

        var conversation = await store.FindAsync("conversation-1");

        var completedEvent = Assert.Single(events);
        Assert.Equal("conversation.completed", completedEvent.Type);
        Assert.Empty(turnProvider.SessionRequests);
        Assert.Empty(turnProvider.Requests);
        Assert.Equal(AiConversationStatus.Completed, conversation!.Status);
        Assert.Equal(2, conversation.Messages.Count);
    }

    [Fact(DisplayName = "Chat orchestration resumes persisted tool results on reconnect")]
    public async Task ChatOrchestrationResumesPersistedToolResultsOnReconnect()
    {
        var provider = new InterruptedToolContinuationAiProvider();
        var tool = new EchoTool();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider>(provider);
        services.AddSingleton<IAiTool>(tool);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAiOrchestrator>();
        var store = serviceProvider.GetRequiredService<IAiConversationStore>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                           {
                               ConversationId = "conversation-1",
                               UserId = "user-1",
                               TenantId = "tenant-1",
                               Message = "Use a tool"
                           }))
            {
                // Intentionally drain the stream until the provider interruption is observed.
            }
        });

        var interruptedConversation = await store.FindAsync("conversation-1");
        Assert.Equal(AiConversationStatus.Active, interruptedConversation!.Status);
        Assert.Single(interruptedConversation.Messages, x => x.Role == AiMessageRole.Tool);

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool",
                           IsReconnect = true
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var reconnectRequest = provider.Requests.Last();
        var restoredToolMessage = Assert.Single(reconnectRequest.Messages, x => x.Role == AiMessageRole.Tool);
        var completedConversation = await store.FindAsync("conversation-1");

        Assert.Equal("", reconnectRequest.Message);
        Assert.Empty(reconnectRequest.ToolResults);
        Assert.Equal("tool-call-1", restoredToolMessage.Metadata["toolCallId"]!.GetValue<string>());
        Assert.Equal("echo", restoredToolMessage.Metadata["toolName"]!.GetValue<string>());
        Assert.Equal("Echoed", restoredToolMessage.Content);
        Assert.Single(completedConversation!.Messages, x => x.Role == AiMessageRole.User && x.Content == "Use a tool");
        Assert.Single(completedConversation.Messages, x => x.Role == AiMessageRole.Tool);
        Assert.Equal(AiConversationStatus.Completed, completedConversation.Status);
        Assert.Equal(1, tool.ExecutionCount);
    }

    [Fact(DisplayName = "Chat orchestration does not load foreign tenant conversation history")]
    public async Task ChatOrchestrationDoesNotLoadForeignTenantConversationHistory()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAiOrchestrator>();
        var store = serviceProvider.GetRequiredService<IAiConversationStore>();

        await store.SaveAsync(new AiConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-a",
            UserId = "user-a",
            Status = AiConversationStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AiMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AiMessageRole.User,
                    Content = "Tenant A secret",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           TenantId = "tenant-b",
                           UserId = "user-b",
                           Message = "Continue"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var request = Assert.Single(provider.Requests);
        var original = await store.FindAsync("conversation-1");

        Assert.DoesNotContain(request.Messages, x => x.Content == "Tenant A secret");
        Assert.Equal("tenant-a", original!.TenantId);
        Assert.Single(original.Messages);
    }

    [Fact(DisplayName = "Chat orchestration does not load foreign user conversation history")]
    public async Task ChatOrchestrationDoesNotLoadForeignUserConversationHistory()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAiOrchestrator>();
        var store = serviceProvider.GetRequiredService<IAiConversationStore>();

        await store.SaveAsync(new AiConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-1",
            UserId = "user-a",
            Status = AiConversationStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AiMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AiMessageRole.User,
                    Content = "User A secret",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           TenantId = "tenant-1",
                           UserId = "user-b",
                           Message = "Continue"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var request = Assert.Single(provider.Requests);
        var original = await store.FindAsync("conversation-1");

        Assert.DoesNotContain(request.Messages, x => x.Content == "User A secret");
        Assert.Equal("user-a", original!.UserId);
        Assert.Single(original.Messages);
    }

    [Fact(DisplayName = "Chat orchestration limits resolved context payloads")]
    public async Task ChatOrchestrationLimitsResolvedContextPayloads()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices(options => options.MaxResolvedContextBytes = 64);
        services.AddSingleton<IAiProvider>(provider);
        services.AddSingleton<IAiContextProvider, LargeContextProvider>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAiOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow",
                           Attachments = [new AiContextAttachment { Kind = LargeContextProvider.ContextKind, ReferenceId = "workflow-1" }]
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var context = Assert.Single(provider.Requests.Single().Context);

        Assert.Equal(64, context.Summary.Length);
        Assert.True(context.Data["truncated"]!.GetValue<bool>());
        Assert.Equal(64, context.Data["maxBytes"]!.GetValue<int>());
        Assert.True(context.Metadata["truncated"]!.GetValue<bool>());
    }

    [Fact(DisplayName = "Chat orchestration truncates multibyte context to the byte limit")]
    public async Task ChatOrchestrationTruncatesMultibyteContextToTheByteLimit()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices(options => options.MaxResolvedContextBytes = 64);
        services.AddSingleton<IAiProvider>(provider);
        services.AddSingleton<IAiContextProvider, MultibyteContextProvider>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAiOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow",
                           Attachments = [new AiContextAttachment { Kind = MultibyteContextProvider.ContextKind, ReferenceId = "workflow-1" }]
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var context = Assert.Single(provider.Requests.Single().Context);

        Assert.True(Encoding.UTF8.GetByteCount(context.Summary) <= 64);
        Assert.True(context.Summary.Length > 16);
    }

    [Fact(DisplayName = "Chat orchestration applies one total resolved context budget")]
    public async Task ChatOrchestrationAppliesOneTotalResolvedContextBudget()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAiHostServices(options => options.MaxResolvedContextBytes = 64);
        services.AddSingleton<IAiProvider>(provider);
        services.AddSingleton<IAiContextProvider, LargeContextProvider>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAiOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow",
                           Attachments =
                           [
                               new AiContextAttachment { Kind = LargeContextProvider.ContextKind, ReferenceId = "workflow-1" },
                               new AiContextAttachment { Kind = LargeContextProvider.ContextKind, ReferenceId = "workflow-2" }
                           ]
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        Assert.Single(provider.Requests.Single().Context);
    }

    [Fact(DisplayName = "Chat orchestration limits tool result payloads")]
    public async Task ChatOrchestrationLimitsToolResultPayloads()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices(options => options.MaxToolResultBytes = 64);
        services.AddSingleton<IAiProvider, ToolCallAiProvider>();
        services.AddSingleton<IAiTool, LargeEchoTool>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
            events.Add(streamEvent);

        var toolResult = Assert.Single(events, x => x.Type == "tool.result");
        var data = toolResult.Data["data"]!.AsObject();

        Assert.Equal(64, toolResult.Data["summary"]!.GetValue<string>().Length);
        Assert.True(data["truncated"]!.GetValue<bool>());
        Assert.Equal(64, data["maxBytes"]!.GetValue<int>());
    }

    [Fact(DisplayName = "Chat orchestration persists max tool turn warning")]
    public async Task ChatOrchestrationPersistsMaxToolTurnWarning()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiProvider, EndlessToolCallAiProvider>();
        services.AddSingleton<IAiTool, EchoTool>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAiOrchestrator>();
        var store = provider.GetRequiredService<IAiConversationStore>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AiChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use tools forever"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var conversation = await store.FindAsync("conversation-1");

        Assert.Contains(
            conversation!.Messages,
            x => x.Role == AiMessageRole.Assistant && x.Content == "Tool execution stopped because the provider requested too many continuation turns.");
    }

    private static string? GetToolResultSummary(AiTurnRequest request) =>
        request.ToolResults.FirstOrDefault()?.Result.Summary ??
        request.Messages.LastOrDefault(x => x.Role == AiMessageRole.Tool)?.Content;

    private class SequencedAiProvider : IAiProvider
    {
        public string Name => "sequenced";

        public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiSessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            yield return new AiProviderEvent
            {
                Type = "assistant.delta",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow
            };

            yield return new AiProviderEvent
            {
                Type = "assistant.delta",
                Sequence = 2,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    private class NamedAiProvider(string name) : IAiProvider
    {
        public string Name => name;

        public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiSessionHandle { Id = request.ConversationId });

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
                    ["provider"] = name
                }
            };
        }
    }

    private class CapturingTurnProvider : IAiProvider
    {
        public string Name => "capturing";
        public List<CreateAiSessionRequest> SessionRequests { get; } = [];
        public List<AiTurnRequest> Requests { get; } = [];

        public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default)
        {
            SessionRequests.Add(request);
            return ValueTask.FromResult(new AiSessionHandle
            {
                Id = request.ConversationId,
                ProviderSessionId = $"provider-session-{request.ConversationId}"
            });
        }

        public async IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            Requests.Add(request);

            yield return new AiProviderEvent
            {
                Type = "assistant.delta",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow,
                Data = new JsonObject
                {
                    ["content"] = "Captured"
                }
            };
        }
    }

    private class ThrowingSessionProvider : IAiProvider
    {
        public string Name => "throwing-session";

        public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Session creation failed.");

        public async IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield break;
        }
    }

    private class DefaultSessionHandleProvider : IAiProvider
    {
        public string Name => "default-session-handle";
        public List<CreateAiSessionRequest> SessionRequests { get; } = [];

        public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default)
        {
            SessionRequests.Add(request);
            return ValueTask.FromResult(new AiSessionHandle());
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
                    ["content"] = "Captured"
                }
            };
        }
    }

    private class LargeContextProvider : IAiContextProvider
    {
        public const string ContextKind = "LargeContext";
        public string Kind => ContextKind;

        public ValueTask<AiResolvedContext> ResolveAsync(AiContextResolutionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiResolvedContext
            {
                Kind = ContextKind,
                ReferenceId = request.Attachment.ReferenceId,
                Summary = new string('s', 512),
                Data = new JsonObject { ["content"] = new string('d', 512) },
                Metadata = new JsonObject { ["content"] = new string('m', 512) }
            });
    }

    private class MultibyteContextProvider : IAiContextProvider
    {
        public const string ContextKind = "MultibyteContext";
        public string Kind => ContextKind;

        public ValueTask<AiResolvedContext> ResolveAsync(AiContextResolutionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiResolvedContext
            {
                Kind = ContextKind,
                ReferenceId = request.Attachment.ReferenceId,
                Summary = new string('漢', 512)
            });
    }

    private class ThrowingContextProvider : IAiContextProvider
    {
        public const string ContextKind = "ThrowingContext";
        public string Kind => ContextKind;

        public ValueTask<AiResolvedContext> ResolveAsync(AiContextResolutionRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Context unavailable.");
    }

    private class ToolContinuationAiProvider : IAiProvider
    {
        public string Name => "tool-continuation";
        public List<AiTurnRequest> Requests { get; } = [];

        public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiSessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            Requests.Add(request);
            var toolResultSummary = GetToolResultSummary(request);

            if (toolResultSummary == null)
            {
                yield return new AiProviderEvent
                {
                    Type = "tool.call",
                    Sequence = 1,
                    Timestamp = DateTimeOffset.UtcNow,
                    Data = new JsonObject
                    {
                        ["id"] = "tool-call-1",
                        ["toolName"] = "echo",
                        ["arguments"] = new JsonObject
                        {
                            ["text"] = "hello"
                        }
                    }
                };

                yield break;
            }

            yield return new AiProviderEvent
            {
                Type = "assistant.delta",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow,
                Data = new JsonObject
                {
                    ["content"] = $"Used {toolResultSummary}"
                }
            };
        }
    }

    private class ToolCallAiProvider : IAiProvider
    {
        public string Name => "tool-caller";

        public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiSessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            yield return new AiProviderEvent
            {
                Type = "tool.call",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow,
                Data = new JsonObject
                {
                    ["id"] = "tool-call-1",
                    ["toolName"] = "echo",
                    ["arguments"] = new JsonObject
                    {
                        ["text"] = "hello"
                    }
                }
            };
        }
    }

    private class EndlessToolCallAiProvider : IAiProvider
    {
        private int _index;

        public string Name => "endless-tool-caller";

        public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiSessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var id = Interlocked.Increment(ref _index);

            yield return new AiProviderEvent
            {
                Type = "tool.call",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow,
                Data = new JsonObject
                {
                    ["id"] = $"tool-call-{id}",
                    ["toolName"] = "echo",
                    ["arguments"] = new JsonObject
                    {
                        ["text"] = "hello"
                    }
                }
            };
        }
    }

    private class InterruptedToolContinuationAiProvider : IAiProvider
    {
        private bool _throwOnFirstContinuation = true;

        public string Name => "interrupted-tool-continuation";
        public List<AiTurnRequest> Requests { get; } = [];

        public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiSessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            Requests.Add(request);
            var toolResultSummary = GetToolResultSummary(request);

            if (toolResultSummary == null)
            {
                yield return new AiProviderEvent
                {
                    Type = "tool.call",
                    Sequence = 1,
                    Timestamp = DateTimeOffset.UtcNow,
                    Data = new JsonObject
                    {
                        ["id"] = "tool-call-1",
                        ["toolName"] = "echo",
                        ["arguments"] = new JsonObject
                        {
                            ["text"] = "hello"
                        }
                    }
                };

                yield break;
            }

            if (_throwOnFirstContinuation)
            {
                _throwOnFirstContinuation = false;
                throw new InvalidOperationException("Stream interrupted.");
            }

            yield return new AiProviderEvent
            {
                Type = "assistant.delta",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow,
                Data = new JsonObject
                {
                    ["content"] = $"Used {toolResultSummary}"
                }
            };
        }
    }

    private class EchoTool : IAiTool
    {
        public int ExecutionCount { get; private set; }

        public AiToolDefinition Definition { get; } = new()
        {
            Name = "echo",
            DisplayName = "Echo",
            TenantBehavior = AiTenantBehavior.TenantScoped,
            EnabledByDefault = true
        };

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default)
        {
            ExecutionCount++;

            return ValueTask.FromResult(new AiToolResult
            {
                Summary = "Echoed",
                Data = new JsonObject
                {
                    ["text"] = context.Arguments["text"]?.GetValue<string>()
                }
            });
        }
    }

    private class ThrowingTool : IAiTool
    {
        public AiToolDefinition Definition { get; } = new()
        {
            Name = "echo",
            DisplayName = "Echo",
            TenantBehavior = AiTenantBehavior.TenantScoped,
            EnabledByDefault = true
        };

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Sensitive internal tool failure.");
    }

    private class DelayedEchoTool : IAiTool
    {
        public AiToolDefinition Definition { get; } = new()
        {
            Name = "echo",
            DisplayName = "Echo",
            TenantBehavior = AiTenantBehavior.TenantScoped,
            EnabledByDefault = true
        };

        public async ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(20, cancellationToken);
            return new AiToolResult
            {
                Summary = "Echoed",
                Data = new JsonObject
                {
                    ["text"] = context.Arguments["text"]?.GetValue<string>()
                }
            };
        }
    }

    private class LargeEchoTool : IAiTool
    {
        public AiToolDefinition Definition { get; } = new()
        {
            Name = "echo",
            DisplayName = "Echo",
            TenantBehavior = AiTenantBehavior.TenantScoped,
            EnabledByDefault = true
        };

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AiToolResult
            {
                Summary = new string('s', 512),
                Data = new JsonObject
                {
                    ["content"] = new string('d', 512)
                }
            });
        }
    }

    private class CapturingAuditSink : IAiAuditSink
    {
        public List<AiAuditEvent> Events { get; } = [];

        public ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }

    private class ThrowingAuditSink : IAiAuditSink
    {
        public ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Audit sink unavailable.");
    }

    private class ThrowingConversationStore : IAiConversationStore
    {
        public ValueTask<AiConversation?> FindAsync(string id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AiConversation?>(null);

        public ValueTask SaveAsync(AiConversation conversation, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Conversation store unavailable.");
    }
}
