using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text;
using System.Text.Json.Nodes;

namespace Elsa.AI.IntegrationTests;

public class AIChatEndpointTests
{
    [Fact(DisplayName = "Chat orchestration emits conversation and assistant events")]
    public async Task ChatOrchestrationEmitsConversationAndAssistantEvents()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider, SequencedAIProvider>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider>(new NamedAIProvider("first"));
        services.AddSingleton<IAIProvider>(new NamedAIProvider("second"));
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices(options =>
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
        services.AddSingleton<IAIProvider>(new NamedAIProvider("first"));
        services.AddSingleton<IAIProvider>(new NamedAIProvider("second"));
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices(options =>
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
        services.AddSingleton<IAIProvider>(new NamedAIProvider("disabled"));
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices(options =>
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
        services.AddSingleton<IAIProvider>(capturingProvider);
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices();
        services.RemoveAll<IAIAuditSink>();
        services.AddSingleton<IAIAuditSink>(auditSink);
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices();
        services.RemoveAll<IAIAuditSink>();
        services.AddSingleton<IAIAuditSink, ThrowingAuditSink>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices();
        services.RemoveAll<IAIConversationStore>();
        services.AddSingleton<IAIConversationStore, ThrowingConversationStore>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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

    [Fact(DisplayName = "Chat orchestration skips conversation store when persistence is disabled")]
    public async Task ChatOrchestrationSkipsConversationStoreWhenPersistenceIsDisabled()
    {
        var conversationStore = new TrackingConversationStore();
        var services = new ServiceCollection();
        services.AddAIHostServices(options => options.ConversationPersistenceEnabled = false);
        services.RemoveAll<IAIConversationStore>();
        services.AddSingleton<IAIConversationStore>(conversationStore);
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
            events.Add(streamEvent);

        Assert.Contains(events, x => x.Type == "conversation.completed");
        Assert.Equal(0, conversationStore.FindCount);
        Assert.Equal(0, conversationStore.SaveCount);
    }

    [Fact(DisplayName = "Chat orchestration emits terminal events when conversation lookup fails")]
    public async Task ChatOrchestrationEmitsTerminalEventsWhenConversationLookupFails()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.RemoveAll<IAIConversationStore>();
        services.AddSingleton<IAIConversationStore, ThrowingFindConversationStore>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
            events.Add(streamEvent);

        Assert.Contains(events, x => x.Type == "conversation.started");
        Assert.Contains(events, x => x.Type == "conversation.error");
        Assert.Contains(events, x => x.Type == "conversation.completed");
    }


    [Fact(DisplayName = "Chat orchestration emits terminal events when context resolution fails")]
    public async Task ChatOrchestrationEmitsTerminalEventsWhenContextResolutionFails()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIContextProvider, ThrowingContextProvider>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var store = provider.GetRequiredService<IAIConversationStore>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Explain this workflow",
                           Attachments = [new AIContextAttachment { Kind = ThrowingContextProvider.ContextKind, ReferenceId = "workflow-1" }]
                       }))
            events.Add(streamEvent);

        var conversation = await store.FindAsync("conversation-1");

        Assert.Contains(events, x => x.Type == "conversation.started");
        Assert.Contains(events, x => x.Type == "conversation.error");
        Assert.Contains(events, x => x.Type == "conversation.completed");
        Assert.Equal(AIConversationStatus.Failed, conversation!.Status);
    }

    [Fact(DisplayName = "Chat orchestration emits terminal events when provider session creation fails")]
    public async Task ChatOrchestrationEmitsTerminalEventsWhenProviderSessionCreationFails()
    {
        var auditSink = new CapturingAuditSink();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider, ThrowingSessionProvider>();
        services.RemoveAll<IAIAuditSink>();
        services.AddSingleton<IAIAuditSink>(auditSink);
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var store = provider.GetRequiredService<IAIConversationStore>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        Assert.Equal(AIConversationStatus.Failed, conversation!.Status);
        Assert.Contains(auditSink.Events, x => x.Type == "chat.failed");
    }

    [Fact(DisplayName = "Chat orchestration emits terminal events when provider turn fails")]
    public async Task ChatOrchestrationEmitsTerminalEventsWhenProviderTurnFails()
    {
        var auditSink = new CapturingAuditSink();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider, ThrowingTurnProvider>();
        services.RemoveAll<IAIAuditSink>();
        services.AddSingleton<IAIAuditSink>(auditSink);
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var store = provider.GetRequiredService<IAIConversationStore>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        Assert.Equal(AIConversationStatus.Failed, conversation!.Status);
        Assert.Contains(auditSink.Events, x => x.Type == "chat.failed");
    }

    [Fact(DisplayName = "Chat orchestration executes provider tool calls")]
    public async Task ChatOrchestrationExecutesProviderToolCalls()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider, ToolCallAIProvider>();
        services.AddSingleton<IAITool, EchoTool>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
            events.Add(streamEvent);

        var toolResult = Assert.Single(events, x => x.Type == "tool.result");
        Assert.Equal("echo", toolResult.Data["toolName"]!.GetValue<string>());
        Assert.Equal(AIToolInvocationStatus.Completed.ToString(), toolResult.Data["status"]!.GetValue<string>());
        Assert.Equal("Echoed", toolResult.Data["summary"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Chat orchestration sends only enabled tools to providers")]
    public async Task ChatOrchestrationSendsOnlyEnabledToolsToProviders()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider>(provider);
        services.AddSingleton<IAITool, DisabledEchoTool>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var tools = provider.Requests.Single().Tools;
        Assert.DoesNotContain(tools, x => x.Name == "disabled-echo");
        Assert.Contains(tools, x => x.Name == "activities.search");
    }

    [Fact(DisplayName = "Chat orchestration audits unresolved tool calls")]
    public async Task ChatOrchestrationAuditsUnresolvedToolCalls()
    {
        var auditSink = new CapturingAuditSink();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.RemoveAll<IAIAuditSink>();
        services.AddSingleton<IAIAuditSink>(auditSink);
        services.AddSingleton<IAIProvider, UnknownToolAIProvider>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices();
        services.RemoveAll<IAIAuditSink>();
        services.AddSingleton<IAIAuditSink>(auditSink);
        services.AddSingleton<IAIProvider, ToolCallAIProvider>();
        services.AddSingleton<IAITool, DelayedEchoTool>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider, ToolCallAIProvider>();
        services.AddSingleton<IAITool, ThrowingTool>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
            events.Add(streamEvent);

        var toolResult = Assert.Single(events, x => x.Type == "tool.result");
        Assert.Equal(AIToolInvocationStatus.Failed.ToString(), toolResult.Data["status"]!.GetValue<string>());
        Assert.Equal("Tool execution failed.", toolResult.Data["error"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Chat orchestration lets providers own tool continuation")]
    public async Task ChatOrchestrationLetsProvidersOwnToolContinuation()
    {
        var provider = new ToolCallAIProvider();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider>(provider);
        services.AddSingleton<IAITool, EchoTool>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
            events.Add(streamEvent);

        var request = Assert.Single(provider.Requests);

        Assert.DoesNotContain(request.Messages, x => x.Role == AIMessageRole.Tool);
        Assert.Contains(events, x => x.Type == "assistant.delta" && x.Data["content"]!.GetValue<string>() == "Used Echoed");
    }

    [Fact(DisplayName = "Chat orchestration persists conversation state")]
    public async Task ChatOrchestrationPersistsConversationState()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var store = provider.GetRequiredService<IAIConversationStore>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        Assert.Equal(AIConversationStatus.Completed, conversation.Status);
        Assert.NotNull(conversation.RetentionExpiresAt);
        Assert.True(conversation.Messages.Single(x => x.Role == AIMessageRole.User).StreamSequence > 0);
        Assert.Contains(conversation.Messages, x => x.Role == AIMessageRole.User && x.Content == "Explain this workflow");
    }

    [Fact(DisplayName = "Chat orchestration preserves conversation title")]
    public async Task ChatOrchestrationPreservesConversationTitle()
    {
        var now = DateTimeOffset.UtcNow;
        var services = new ServiceCollection();
        services.AddAIHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var store = provider.GetRequiredService<IAIConversationStore>();
        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            Title = "Workflow assistant",
            Status = AIConversationStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        });

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Explain this workflow"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var conversation = await store.FindAsync("conversation-1");

        Assert.Equal("Workflow assistant", conversation!.Title);
    }

    [Fact(DisplayName = "Chat orchestration creates provider sessions")]
    public async Task ChatOrchestrationCreatesProviderSessions()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();
        var store = serviceProvider.GetRequiredService<IAIConversationStore>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "First"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "First"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        Assert.Contains(secondRequest.Messages, x => x.Role == AIMessageRole.User && x.Content == "First");
        Assert.Contains(secondRequest.Messages, x => x.Role == AIMessageRole.Assistant);
        Assert.DoesNotContain(secondRequest.Messages, x => x.Role == AIMessageRole.User && x.Content == "Second");
    }

    [Fact(DisplayName = "Chat orchestration does not duplicate reconnect user messages")]
    public async Task ChatOrchestrationDoesNotDuplicateReconnectUserMessages()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var store = provider.GetRequiredService<IAIConversationStore>();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            Status = AIConversationStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AIMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AIMessageRole.User,
                    Content = "Retry me",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        var userMessages = conversation!.Messages.Where(x => x.Role == AIMessageRole.User && x.Content == "Retry me").ToList();

        Assert.Single(userMessages);
    }

    [Fact(DisplayName = "Chat orchestration completes failed reconnects without duplicating user messages")]
    public async Task ChatOrchestrationCompletesFailedReconnectsWithoutDuplicatingUserMessages()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var store = provider.GetRequiredService<IAIConversationStore>();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-failed",
            UserId = "user-1",
            Status = AIConversationStatus.Failed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AIMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-failed",
                    Role = AIMessageRole.User,
                    Content = "Retry me",
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new AIMessage
                {
                    Id = "message-2",
                    ConversationId = "conversation-failed",
                    Role = AIMessageRole.Assistant,
                    Content = "Weaver could not prepare AI context or tools for this request.",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });

        var events = new List<AIStreamEvent>();
        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           ConversationId = "conversation-failed",
                           UserId = "user-1",
                           Message = "Retry me\r\n",
                           IsReconnect = true
                       }))
            events.Add(streamEvent);

        var conversation = await store.FindAsync("conversation-failed");
        var userMessages = conversation!.Messages.Where(x => x.Role == AIMessageRole.User && x.Content == "Retry me").ToList();

        Assert.Collection(
            events,
            streamEvent =>
            {
                Assert.Equal("conversation.error", streamEvent.Type);
                Assert.Equal("Weaver could not prepare AI context or tools for this request.", streamEvent.Data["content"]!.GetValue<string>());
            },
            streamEvent => Assert.Equal("conversation.completed", streamEvent.Type));
        Assert.Single(userMessages);
    }

    [Fact(DisplayName = "Chat orchestration continues reconnect sequences after persisted messages")]
    public async Task ChatOrchestrationContinuesReconnectSequencesAfterPersistedMessages()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var store = provider.GetRequiredService<IAIConversationStore>();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            Status = AIConversationStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AIMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AIMessageRole.User,
                    Content = "Retry me",
                    CreatedAt = DateTimeOffset.UtcNow,
                    StreamSequence = 3
                }
            ]
        });

        var events = new List<AIStreamEvent>();
        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           Message = "Retry me",
                           IsReconnect = true
                       }))
            events.Add(streamEvent);

        var startedEvent = Assert.Single(events, x => x.Type == "conversation.started");
        var assistantEvent = Assert.Single(events, x => x.Type == "assistant.delta");
        Assert.Equal(4, startedEvent.Sequence);
        Assert.Equal(5, assistantEvent.Sequence);
    }

    [Fact(DisplayName = "Chat orchestration starts a new conversation when reconnect history is unavailable")]
    public async Task ChatOrchestrationStartsNewConversationWhenReconnectHistoryIsUnavailable()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           ConversationId = "missing-conversation",
                           UserId = "user-1",
                           Message = "Retry me",
                           IsReconnect = true
                       }))
            events.Add(streamEvent);

        var startedEvent = Assert.Single(events, x => x.Type == "conversation.started");

        Assert.NotEqual("missing-conversation", startedEvent.ConversationId);
        Assert.Equal(0, startedEvent.Sequence);
    }

    [Fact(DisplayName = "Chat orchestration does not replay completed conversations on reconnect")]
    public async Task ChatOrchestrationDoesNotReplayCompletedConversationsOnReconnect()
    {
        var turnProvider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider>(turnProvider);
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var store = provider.GetRequiredService<IAIConversationStore>();
        var events = new List<AIStreamEvent>();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            Status = AIConversationStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AIMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AIMessageRole.User,
                    Content = "Retry me",
                    CreatedAt = DateTimeOffset.UtcNow,
                    StreamSequence = 0
                },
                new AIMessage
                {
                    Id = "message-2",
                    ConversationId = "conversation-1",
                    Role = AIMessageRole.Assistant,
                    Content = "Done",
                    CreatedAt = DateTimeOffset.UtcNow,
                    StreamSequence = 1
                }
            ]
        });

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        Assert.Equal(AIConversationStatus.Completed, conversation!.Status);
        Assert.Equal(2, conversation.Messages.Count);
    }

    [Fact(DisplayName = "Chat orchestration sends persisted history on reconnect")]
    public async Task ChatOrchestrationSendsPersistedHistoryOnReconnect()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();
        var store = serviceProvider.GetRequiredService<IAIConversationStore>();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-1",
            UserId = "user-1",
            Status = AIConversationStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ProviderSessionId = "provider-session-conversation-1",
            Messages =
            [
                new AIMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AIMessageRole.User,
                    Content = "Use a tool",
                    CreatedAt = DateTimeOffset.UtcNow,
                    StreamSequence = 0
                },
                new AIMessage
                {
                    Id = "message-2",
                    ConversationId = "conversation-1",
                    Role = AIMessageRole.Assistant,
                    Content = "",
                    CreatedAt = DateTimeOffset.UtcNow,
                    StreamSequence = 1,
                    Metadata = new JsonObject
                    {
                        ["toolCallIds"] = new JsonArray("tool-call-1")
                    }
                },
                new AIMessage
                {
                    Id = "message-3",
                    ConversationId = "conversation-1",
                    Role = AIMessageRole.Tool,
                    Content = "Echoed",
                    CreatedAt = DateTimeOffset.UtcNow,
                    StreamSequence = 2,
                    Metadata = new JsonObject
                    {
                        ["toolCallId"] = "tool-call-1",
                        ["toolName"] = "echo",
                        ["status"] = AIToolInvocationStatus.Completed.ToString()
                    }
                }
            ]
        });

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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

        var reconnectRequest = Assert.Single(provider.Requests);
        var restoredToolMessage = Assert.Single(reconnectRequest.Messages, x => x.Role == AIMessageRole.Tool);
        var completedConversation = await store.FindAsync("conversation-1");

        Assert.Equal("", reconnectRequest.Message);
        Assert.Equal("tool-call-1", restoredToolMessage.Metadata["toolCallId"]!.GetValue<string>());
        Assert.Equal("echo", restoredToolMessage.Metadata["toolName"]!.GetValue<string>());
        Assert.Equal("Echoed", restoredToolMessage.Content);
        Assert.Single(completedConversation!.Messages, x => x.Role == AIMessageRole.User && x.Content == "Use a tool");
        Assert.Single(completedConversation.Messages, x => x.Role == AIMessageRole.Tool);
        Assert.Equal(AIConversationStatus.Completed, completedConversation.Status);
    }

    [Fact(DisplayName = "Chat orchestration does not load foreign tenant conversation history")]
    public async Task ChatOrchestrationDoesNotLoadForeignTenantConversationHistory()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();
        var store = serviceProvider.GetRequiredService<IAIConversationStore>();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-a",
            UserId = "user-a",
            Status = AIConversationStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AIMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AIMessageRole.User,
                    Content = "Tenant A secret",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();
        var store = serviceProvider.GetRequiredService<IAIConversationStore>();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-1",
            UserId = "user-a",
            Status = AIConversationStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AIMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AIMessageRole.User,
                    Content = "User A secret",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
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
        services.AddAIHostServices(options => options.MaxResolvedContextBytes = 64);
        services.AddSingleton<IAIProvider>(provider);
        services.AddSingleton<IAIContextProvider, LargeContextProvider>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow",
                           Attachments = [new AIContextAttachment { Kind = LargeContextProvider.ContextKind, ReferenceId = "workflow-1" }]
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
        services.AddAIHostServices(options => options.MaxResolvedContextBytes = 64);
        services.AddSingleton<IAIProvider>(provider);
        services.AddSingleton<IAIContextProvider, MultibyteContextProvider>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow",
                           Attachments = [new AIContextAttachment { Kind = MultibyteContextProvider.ContextKind, ReferenceId = "workflow-1" }]
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var context = Assert.Single(provider.Requests.Single().Context);

        Assert.True(Encoding.UTF8.GetByteCount(context.Summary) <= 64);
        Assert.True(context.Summary.Length > 16);
    }

    [Fact(DisplayName = "Chat orchestration does not split surrogate pairs when truncating context")]
    public async Task ChatOrchestrationDoesNotSplitSurrogatePairsWhenTruncatingContext()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAIHostServices(options => options.MaxResolvedContextBytes = 3);
        services.AddSingleton<IAIProvider>(provider);
        services.AddSingleton<IAIContextProvider, SurrogatePairContextProvider>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow",
                           Attachments = [new AIContextAttachment { Kind = SurrogatePairContextProvider.ContextKind, ReferenceId = "workflow-1" }]
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var context = Assert.Single(provider.Requests.Single().Context);

        Assert.False(context.Summary.Length > 0 && char.IsHighSurrogate(context.Summary[^1]));
        Assert.True(Encoding.UTF8.GetByteCount(context.Summary) <= 3);
    }

    [Fact(DisplayName = "Chat orchestration applies one total resolved context budget")]
    public async Task ChatOrchestrationAppliesOneTotalResolvedContextBudget()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAIHostServices(options => options.MaxResolvedContextBytes = 64);
        services.AddSingleton<IAIProvider>(provider);
        services.AddSingleton<IAIContextProvider, LargeContextProvider>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow",
                           Attachments =
                           [
                               new AIContextAttachment { Kind = LargeContextProvider.ContextKind, ReferenceId = "workflow-1" },
                               new AIContextAttachment { Kind = LargeContextProvider.ContextKind, ReferenceId = "workflow-2" }
                           ]
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        Assert.Single(provider.Requests.Single().Context);
    }

    [Fact(DisplayName = "Chat orchestration keeps smaller contexts after an oversized context")]
    public async Task ChatOrchestrationKeepsSmallerContextsAfterOversizedContext()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAIHostServices(options => options.MaxResolvedContextBytes = 1024);
        services.AddSingleton<IAIProvider>(provider);
        services.AddSingleton<IAIContextProvider, MixedSizeContextProvider>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain these workflows",
                           Attachments =
                           [
                               new AIContextAttachment { Kind = MixedSizeContextProvider.ContextKind, ReferenceId = "small-1" },
                               new AIContextAttachment { Kind = MixedSizeContextProvider.ContextKind, ReferenceId = "large" },
                               new AIContextAttachment { Kind = MixedSizeContextProvider.ContextKind, ReferenceId = "small-2" }
                           ]
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var contexts = provider.Requests.Single().Context.ToList();

        Assert.Collection(
            contexts,
            first => Assert.Equal("small-1", first.ReferenceId),
            second => Assert.Equal("small-2", second.ReferenceId));
    }

    [Fact(DisplayName = "Chat orchestration treats non-positive context byte limit as unlimited")]
    public async Task ChatOrchestrationTreatsNonPositiveContextByteLimitAsUnlimited()
    {
        var provider = new CapturingTurnProvider();
        var services = new ServiceCollection();
        services.AddAIHostServices(options => options.MaxResolvedContextBytes = 0);
        services.AddSingleton<IAIProvider>(provider);
        services.AddSingleton<IAIContextProvider, LargeContextProvider>();
        using var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<IAIOrchestrator>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           UserId = "user-1",
                           Message = "Explain this workflow",
                           Attachments = [new AIContextAttachment { Kind = LargeContextProvider.ContextKind, ReferenceId = "workflow-1" }]
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var context = Assert.Single(provider.Requests.Single().Context);

        Assert.Equal(512, context.Summary.Length);
        Assert.False(context.Data.ContainsKey("truncated"));
    }

    [Fact(DisplayName = "Chat orchestration limits tool result payloads")]
    public async Task ChatOrchestrationLimitsToolResultPayloads()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices(options => options.MaxToolResultBytes = 64);
        services.AddSingleton<IAIProvider, ToolCallAIProvider>();
        services.AddSingleton<IAITool, LargeEchoTool>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var events = new List<AIStreamEvent>();

        await foreach (var streamEvent in orchestrator.ExecuteChatAsync(new AIChatRequest
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

    [Fact(DisplayName = "Chat orchestration persists provider-emitted tool results")]
    public async Task ChatOrchestrationPersistsProviderEmittedToolResults()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProvider, ProviderToolEventAIProvider>();
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAIOrchestrator>();
        var store = provider.GetRequiredService<IAIConversationStore>();

        await foreach (var _ in orchestrator.ExecuteChatAsync(new AIChatRequest
                       {
                           ConversationId = "conversation-1",
                           UserId = "user-1",
                           TenantId = "tenant-1",
                           Message = "Use a tool"
                       }))
        {
            // Intentionally drain the stream to completion.
        }

        var conversation = await store.FindAsync("conversation-1");
        var toolMessage = Assert.Single(conversation!.Messages, x => x.Role == AIMessageRole.Tool);

        Assert.Equal("tool-call-1", toolMessage.Metadata["toolCallId"]!.GetValue<string>());
        Assert.Equal("echo", toolMessage.Metadata["toolName"]!.GetValue<string>());
        Assert.Equal("Echoed", toolMessage.Content);
    }

    private class SequencedAIProvider : IAIProvider
    {
        public string Name => "sequenced";

        public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AISessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            yield return new AIProviderEvent
            {
                Type = "assistant.delta",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow
            };

            yield return new AIProviderEvent
            {
                Type = "assistant.delta",
                Sequence = 2,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    private class NamedAIProvider(string name) : IAIProvider
    {
        public string Name => name;

        public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AISessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            yield return new AIProviderEvent
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

    private class CapturingTurnProvider : IAIProvider
    {
        public string Name => "capturing";
        public List<CreateAISessionRequest> SessionRequests { get; } = [];
        public List<AITurnRequest> Requests { get; } = [];

        public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default)
        {
            SessionRequests.Add(request);
            return ValueTask.FromResult(new AISessionHandle
            {
                Id = request.ConversationId,
                ProviderSessionId = $"provider-session-{request.ConversationId}"
            });
        }

        public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            Requests.Add(request);

            yield return new AIProviderEvent
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

    private class ThrowingSessionProvider : IAIProvider
    {
        public string Name => "throwing-session";

        public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Session creation failed.");

        public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield break;
        }
    }

    private class ThrowingTurnProvider : IAIProvider
    {
        public string Name => "throwing-turn";

        public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AISessionHandle
            {
                Id = request.ConversationId,
                ProviderSessionId = $"provider-session-{request.ConversationId}"
            });

        public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (!cancellationToken.IsCancellationRequested)
                throw new InvalidOperationException("Provider unavailable.");

            yield break;
        }
    }

    private class DefaultSessionHandleProvider : IAIProvider
    {
        public string Name => "default-session-handle";
        public List<CreateAISessionRequest> SessionRequests { get; } = [];

        public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default)
        {
            SessionRequests.Add(request);
            return ValueTask.FromResult(new AISessionHandle());
        }

        public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            yield return new AIProviderEvent
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

    private class LargeContextProvider : IAIContextProvider
    {
        public const string ContextKind = "LargeContext";
        public string Kind => ContextKind;

        public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIResolvedContext
            {
                Kind = ContextKind,
                ReferenceId = request.Attachment.ReferenceId,
                Summary = new string('s', 512),
                Data = new JsonObject { ["content"] = new string('d', 512) },
                Metadata = new JsonObject { ["content"] = new string('m', 512) }
            });
    }

    private class MultibyteContextProvider : IAIContextProvider
    {
        public const string ContextKind = "MultibyteContext";
        public string Kind => ContextKind;

        public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIResolvedContext
            {
                Kind = ContextKind,
                ReferenceId = request.Attachment.ReferenceId,
                Summary = new string('漢', 512)
            });
    }

    private class SurrogatePairContextProvider : IAIContextProvider
    {
        public const string ContextKind = "SurrogatePairContext";
        public string Kind => ContextKind;

        public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIResolvedContext
            {
                Kind = ContextKind,
                ReferenceId = request.Attachment.ReferenceId,
                Summary = "😀x"
            });
    }

    private class MixedSizeContextProvider : IAIContextProvider
    {
        public const string ContextKind = "MixedSizeContext";
        public string Kind => ContextKind;

        public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default)
        {
            var isLarge = request.Attachment.ReferenceId == "large";
            return ValueTask.FromResult(new AIResolvedContext
            {
                Kind = ContextKind,
                ReferenceId = request.Attachment.ReferenceId,
                Summary = isLarge ? new string('l', 4096) : $"Context {request.Attachment.ReferenceId}"
            });
        }
    }

    private class ThrowingContextProvider : IAIContextProvider
    {
        public const string ContextKind = "ThrowingContext";
        public string Kind => ContextKind;

        public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Context unavailable.");
    }

    private class ToolCallAIProvider : IAIProvider
    {
        public string Name => "tool-caller";
        public List<AITurnRequest> Requests { get; } = [];

        public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AISessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            Requests.Add(request);
            var result = await toolInvoker.InvokeAsync(new AIProviderToolInvocation
            {
                Id = "tool-call-1",
                ToolName = "echo",
                Arguments = new JsonObject
                {
                    ["text"] = "hello"
                }
            }, cancellationToken);

            yield return new AIProviderEvent
            {
                Type = "tool.result",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow,
                Data = new JsonObject
                {
                    ["toolCallId"] = "tool-call-1",
                    ["toolName"] = "echo",
                    ["status"] = result.Status.ToString(),
                    ["summary"] = result.Summary,
                    ["error"] = result.Error,
                    ["data"] = result.Data.DeepClone()
                }
            };

            yield return new AIProviderEvent
            {
                Type = "assistant.delta",
                Sequence = 2,
                Timestamp = DateTimeOffset.UtcNow,
                Data = new JsonObject
                {
                    ["content"] = $"Used {result.Summary}"
                }
            };
        }
    }

    private class UnknownToolAIProvider : IAIProvider
    {
        public string Name => "unknown-tool-caller";

        public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AISessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var result = await toolInvoker.InvokeAsync(new AIProviderToolInvocation
            {
                Id = "tool-call-1",
                ToolName = "echo",
                Arguments = new JsonObject
                {
                    ["text"] = "hello"
                }
            }, cancellationToken);

            yield return new AIProviderEvent
            {
                Type = "tool.result",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow,
                Data = new JsonObject
                {
                    ["toolCallId"] = "tool-call-1",
                    ["toolName"] = "echo",
                    ["status"] = result.Status.ToString(),
                    ["summary"] = result.Summary,
                    ["error"] = result.Error,
                    ["data"] = result.Data.DeepClone()
                }
            };
        }
    }

    private class ProviderToolEventAIProvider : IAIProvider
    {
        public string Name => "provider-tool-event";

        public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AISessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            yield return new AIProviderEvent
            {
                Type = "tool.result",
                Sequence = 1,
                Timestamp = DateTimeOffset.UtcNow,
                Data = new JsonObject
                {
                    ["toolCallId"] = "tool-call-1",
                    ["toolName"] = "echo",
                    ["status"] = AIToolInvocationStatus.Completed.ToString(),
                    ["summary"] = "Echoed",
                    ["data"] = new JsonObject
                    {
                        ["text"] = "hello"
                    }
                }
            };
        }
    }

    private class EchoTool : IAITool
    {
        public int ExecutionCount { get; private set; }

        public AIToolDefinition Definition { get; } = new()
        {
            Name = "echo",
            DisplayName = "Echo",
            TenantBehavior = AITenantBehavior.TenantScoped,
            EnabledByDefault = true
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
        {
            ExecutionCount++;

            return ValueTask.FromResult(new AIToolResult
            {
                Summary = "Echoed",
                Data = new JsonObject
                {
                    ["text"] = context.Arguments["text"]?.GetValue<string>()
                }
            });
        }

        public void Dispose()
        {
        }
    }

    private class DisabledEchoTool : IAITool
    {
        public AIToolDefinition Definition { get; } = new()
        {
            Name = "echo",
            DisplayName = "Echo",
            Mutability = AIToolMutability.Proposal,
            TenantBehavior = AITenantBehavior.TenantScoped
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIToolResult { Summary = "Echoed" });

        public void Dispose()
        {
        }
    }

    private class ThrowingTool : IAITool
    {
        public AIToolDefinition Definition { get; } = new()
        {
            Name = "echo",
            DisplayName = "Echo",
            TenantBehavior = AITenantBehavior.TenantScoped,
            EnabledByDefault = true
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Sensitive internal tool failure.");

        public void Dispose()
        {
        }
    }

    private class DelayedEchoTool : IAITool
    {
        public AIToolDefinition Definition { get; } = new()
        {
            Name = "echo",
            DisplayName = "Echo",
            TenantBehavior = AITenantBehavior.TenantScoped,
            EnabledByDefault = true
        };

        public async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(20, cancellationToken);
            return new AIToolResult
            {
                Summary = "Echoed",
                Data = new JsonObject
                {
                    ["text"] = context.Arguments["text"]?.GetValue<string>()
                }
            };
        }

        public void Dispose()
        {
        }
    }

    private class LargeEchoTool : IAITool
    {
        public AIToolDefinition Definition { get; } = new()
        {
            Name = "echo",
            DisplayName = "Echo",
            TenantBehavior = AITenantBehavior.TenantScoped,
            EnabledByDefault = true
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AIToolResult
            {
                Summary = new string('s', 512),
                Data = new JsonObject
                {
                    ["content"] = new string('d', 512)
                }
            });
        }

        public void Dispose()
        {
        }
    }

    private class CapturingAuditSink : IAIAuditSink
    {
        public List<AIAuditEvent> Events { get; } = [];

        public ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }

    private class ThrowingAuditSink : IAIAuditSink
    {
        public ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Audit sink unavailable.");
    }

    private class ThrowingConversationStore : IAIConversationStore
    {
        public ValueTask<AIConversation?> FindAsync(string id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AIConversation?>(null);

        public ValueTask SaveAsync(AIConversation conversation, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Conversation store unavailable.");
    }

    private class ThrowingFindConversationStore : IAIConversationStore
    {
        public ValueTask<AIConversation?> FindAsync(string id, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Conversation store unavailable.");

        public ValueTask SaveAsync(AIConversation conversation, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }

    private class TrackingConversationStore : IAIConversationStore
    {
        public int FindCount { get; private set; }
        public int SaveCount { get; private set; }

        public ValueTask<AIConversation?> FindAsync(string id, CancellationToken cancellationToken = default)
        {
            FindCount++;
            return ValueTask.FromResult<AIConversation?>(null);
        }

        public ValueTask SaveAsync(AIConversation conversation, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return ValueTask.CompletedTask;
        }
    }
}
