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
    [Test]
    [DisplayName("Chat orchestration emits conversation and assistant events")]
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

        await Assert.That(events).Contains(x => x.Type == "conversation.started");
        await Assert.That(events).Contains(x => x.Type == "assistant.delta");
        await Assert.That(events).Contains(x => x.Type == "conversation.completed");
    }

    [Test]
    [DisplayName("Chat orchestration emits completion after provider sequence")]
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

        var completion = (await Assert.That(events).HasSingleItem(x => x.Type == "conversation.completed"))!;
        await Assert.That(completion.Sequence).IsEqualTo(4);
    }

    [Test]
    [DisplayName("Chat orchestration routes to requested provider")]
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

        var delta = (await Assert.That(events).HasSingleItem(x => x.Type == "assistant.delta"))!;
        await Assert.That(delta.Data["provider"]!.GetValue<string>()).IsEqualTo("second");
    }

    [Test]
    [DisplayName("Chat orchestration routes an agent to its configured provider")]
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

        var delta = (await Assert.That(events).HasSingleItem(x => x.Type == "assistant.delta"))!;
        await Assert.That(delta.Data["provider"]!.GetValue<string>()).IsEqualTo("second");
    }

    [Test]
    [DisplayName("Chat orchestration ignores disabled configured providers")]
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

        var delta = (await Assert.That(events).HasSingleItem(x => x.Type == "assistant.delta"))!;
        await Assert.That(delta.Data["content"]!.GetValue<string>()).IsEqualTo("Weaver is ready, but no AI provider is configured.");
    }

    [Test]
    [DisplayName("Chat orchestration passes selected provider configuration")]
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

        var sessionRequest = (await Assert.That(capturingProvider.SessionRequests).HasSingleItem())!;
        var turnRequest = (await Assert.That(capturingProvider.Requests).HasSingleItem())!;
        await Assert.That(turnRequest.ProviderSessionId).IsEqualTo("provider-session-" + sessionRequest.ConversationId);
        await Assert.That(sessionRequest.ProviderConfiguration!.Name).IsEqualTo("configured");
        await Assert.That(turnRequest.ProviderConfiguration!.Provider).IsEqualTo(capturingProvider.Name);
        await Assert.That(turnRequest.ProviderConfiguration.Model).IsEqualTo("model-1");
        await Assert.That(turnRequest.ProviderConfiguration.ApiKeySecretName).IsEqualTo("secret-1");
        await Assert.That(turnRequest.ProviderConfiguration.Endpoint).IsEqualTo("https://example.local");
    }

    [Test]
    [DisplayName("Chat orchestration records start and completion audit events")]
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

        await Assert.That(auditSink.Events).Count().IsEqualTo(2);
        var started = auditSink.Events[0];
        await Assert.That(started.Type).IsEqualTo("chat.started");
        await Assert.That(started.ConversationId).IsEqualTo("conversation-1");
        await Assert.That(started.TenantId).IsEqualTo("tenant-1");
        await Assert.That(started.ActorId).IsEqualTo("user-1");
        await Assert.That(auditSink.Events[1].Type).IsEqualTo("chat.completed");
    }

    [Test]
    [DisplayName("Chat orchestration continues when audit sink fails")]
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

        await Assert.That(events).Contains(x => x.Type == "conversation.completed");
    }

    [Test]
    [DisplayName("Chat orchestration continues when conversation persistence fails")]
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

        await Assert.That(events).Contains(x => x.Type == "conversation.started");
        await Assert.That(events).Contains(x => x.Type == "assistant.delta");
        await Assert.That(events).Contains(x => x.Type == "conversation.completed");
    }

    [Test]
    [DisplayName("Chat orchestration skips conversation store when persistence is disabled")]
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

        await Assert.That(events).Contains(x => x.Type == "conversation.completed");
        await Assert.That(conversationStore.FindCount).IsEqualTo(0);
        await Assert.That(conversationStore.SaveCount).IsEqualTo(0);
    }

    [Test]
    [DisplayName("Chat orchestration emits terminal events when conversation lookup fails")]
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

        await Assert.That(events).Contains(x => x.Type == "conversation.started");
        await Assert.That(events).Contains(x => x.Type == "conversation.error");
        await Assert.That(events).Contains(x => x.Type == "conversation.completed");
    }


    [Test]
    [DisplayName("Chat orchestration emits terminal events when context resolution fails")]
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

        await Assert.That(events).Contains(x => x.Type == "conversation.started");
        await Assert.That(events).Contains(x => x.Type == "conversation.error");
        await Assert.That(events).Contains(x => x.Type == "conversation.completed");
        await Assert.That(conversation!.Status).IsEqualTo(AIConversationStatus.Failed);
    }

    [Test]
    [DisplayName("Chat orchestration emits terminal events when provider session creation fails")]
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

        await Assert.That(events).Contains(x => x.Type == "conversation.started");
        await Assert.That(events).Contains(x => x.Type == "conversation.error");
        await Assert.That(events).Contains(x => x.Type == "conversation.completed");
        await Assert.That(conversation!.Status).IsEqualTo(AIConversationStatus.Failed);
        await Assert.That(auditSink.Events).Contains(x => x.Type == "chat.failed");
    }

    [Test]
    [DisplayName("Chat orchestration emits terminal events when provider turn fails")]
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

        await Assert.That(events).Contains(x => x.Type == "conversation.started");
        await Assert.That(events).Contains(x => x.Type == "conversation.error");
        await Assert.That(events).Contains(x => x.Type == "conversation.completed");
        await Assert.That(conversation!.Status).IsEqualTo(AIConversationStatus.Failed);
        await Assert.That(auditSink.Events).Contains(x => x.Type == "chat.failed");
    }

    [Test]
    [DisplayName("Chat orchestration executes provider tool calls")]
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

        var toolResult = (await Assert.That(events).HasSingleItem(x => x.Type == "tool.result"))!;
        await Assert.That(toolResult.Data["toolName"]!.GetValue<string>()).IsEqualTo("echo");
        await Assert.That(toolResult.Data["status"]!.GetValue<string>()).IsEqualTo(AIToolInvocationStatus.Completed.ToString());
        await Assert.That(toolResult.Data["summary"]!.GetValue<string>()).IsEqualTo("Echoed");
    }

    [Test]
    [DisplayName("Chat orchestration sends only enabled tools to providers")]
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
        await Assert.That(tools).DoesNotContain(x => x.Name == "disabled-echo");
        await Assert.That(tools).Contains(x => x.Name == "activities.search");
    }

    [Test]
    [DisplayName("Chat orchestration audits unresolved tool calls")]
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

        var toolAudit = (await Assert.That(auditSink.Events).HasSingleItem(x => x.Type == "tool.failed"))!;
        await Assert.That(toolAudit.ToolInvocationId).IsEqualTo("tool-call-1");
        await Assert.That(toolAudit.Data["toolName"]!.GetValue<string>()).IsEqualTo("echo");
    }

    [Test]
    [DisplayName("Chat orchestration records tool audit timestamps around execution")]
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

        var invoked = (await Assert.That(auditSink.Events).HasSingleItem(x => x.Type == "tool.invoked"))!;
        var completed = (await Assert.That(auditSink.Events).HasSingleItem(x => x.Type == "tool.completed"))!;

        await Assert.That(invoked.Timestamp < completed.Timestamp).IsTrue();
    }

    [Test]
    [DisplayName("Chat orchestration redacts tool exception messages from stream events")]
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

        var toolResult = (await Assert.That(events).HasSingleItem(x => x.Type == "tool.result"))!;
        await Assert.That(toolResult.Data["status"]!.GetValue<string>()).IsEqualTo(AIToolInvocationStatus.Failed.ToString());
        await Assert.That(toolResult.Data["error"]!.GetValue<string>()).IsEqualTo("Tool execution failed.");
    }

    [Test]
    [DisplayName("Chat orchestration lets providers own tool continuation")]
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

        var request = (await Assert.That(provider.Requests).HasSingleItem())!;

        await Assert.That(request.Messages).DoesNotContain(x => x.Role == AIMessageRole.Tool);
        await Assert.That(events).Contains(x => x.Type == "assistant.delta" && x.Data["content"]!.GetValue<string>() == "Used Echoed");
    }

    [Test]
    [DisplayName("Chat orchestration persists conversation state")]
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

        conversation = (await Assert.That(conversation).IsNotNull())!;
        await Assert.That(conversation.Status).IsEqualTo(AIConversationStatus.Completed);
        await Assert.That(conversation.RetentionExpiresAt).IsNotNull();
        await Assert.That(conversation.Messages.Single(x => x.Role == AIMessageRole.User).StreamSequence > 0).IsTrue();
        await Assert.That(conversation.Messages).Contains(x => x.Role == AIMessageRole.User && x.Content == "Explain this workflow");
    }

    [Test]
    [DisplayName("Chat orchestration preserves conversation title")]
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

        await Assert.That(conversation!.Title).IsEqualTo("Workflow assistant");
    }

    [Test]
    [DisplayName("Chat orchestration creates provider sessions")]
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

        var sessionRequest = (await Assert.That(provider.SessionRequests).HasSingleItem())!;
        var conversation = await store.FindAsync("conversation-1");

        await Assert.That(sessionRequest.ConversationId).IsEqualTo("conversation-1");
        await Assert.That(conversation!.ProviderSessionId).IsEqualTo("provider-session-conversation-1");
    }

    [Test]
    [DisplayName("Chat orchestration persists generated provider session IDs")]
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

        await Assert.That(provider.SessionRequests).HasSingleItem();
    }

    [Test]
    [DisplayName("Chat orchestration forwards persisted message history")]
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

        await Assert.That(secondRequest.Message).IsEqualTo("Second");
        await Assert.That(secondRequest.Messages).Contains(x => x.Role == AIMessageRole.User && x.Content == "First");
        await Assert.That(secondRequest.Messages).Contains(x => x.Role == AIMessageRole.Assistant);
        await Assert.That(secondRequest.Messages).DoesNotContain(x => x.Role == AIMessageRole.User && x.Content == "Second");
    }

    [Test]
    [DisplayName("Chat orchestration does not duplicate reconnect user messages")]
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

        await Assert.That(userMessages).HasSingleItem();
    }

    [Test]
    [DisplayName("Chat orchestration completes failed reconnects without duplicating user messages")]
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

        await Assert.That(events).Count().IsEqualTo(2);
        await Assert.That(events[0].Type).IsEqualTo("conversation.error");
        await Assert.That(events[0].Data["content"]!.GetValue<string>()).IsEqualTo("Weaver could not prepare AI context or tools for this request.");
        await Assert.That(events[1].Type).IsEqualTo("conversation.completed");
        await Assert.That(userMessages).HasSingleItem();
    }

    [Test]
    [DisplayName("Chat orchestration continues reconnect sequences after persisted messages")]
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

        var startedEvent = (await Assert.That(events).HasSingleItem(x => x.Type == "conversation.started"))!;
        var assistantEvent = (await Assert.That(events).HasSingleItem(x => x.Type == "assistant.delta"))!;
        await Assert.That(startedEvent.Sequence).IsEqualTo(4);
        await Assert.That(assistantEvent.Sequence).IsEqualTo(5);
    }

    [Test]
    [DisplayName("Chat orchestration starts a new conversation when reconnect history is unavailable")]
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

        var startedEvent = (await Assert.That(events).HasSingleItem(x => x.Type == "conversation.started"))!;

        await Assert.That(startedEvent.ConversationId).IsNotEqualTo("missing-conversation");
        await Assert.That(startedEvent.Sequence).IsEqualTo(0);
    }

    [Test]
    [DisplayName("Chat orchestration does not replay completed conversations on reconnect")]
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

        var completedEvent = (await Assert.That(events).HasSingleItem())!;
        await Assert.That(completedEvent.Type).IsEqualTo("conversation.completed");
        await Assert.That(turnProvider.SessionRequests).IsEmpty();
        await Assert.That(turnProvider.Requests).IsEmpty();
        await Assert.That(conversation!.Status).IsEqualTo(AIConversationStatus.Completed);
        await Assert.That(conversation.Messages.Count).IsEqualTo(2);
    }

    [Test]
    [DisplayName("Chat orchestration sends persisted history on reconnect")]
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

        var reconnectRequest = (await Assert.That(provider.Requests).HasSingleItem())!;
        var restoredToolMessage = (await Assert.That(reconnectRequest.Messages).HasSingleItem(x => x.Role == AIMessageRole.Tool))!;
        var completedConversation = await store.FindAsync("conversation-1");

        await Assert.That(reconnectRequest.Message).IsEqualTo("");
        await Assert.That(restoredToolMessage.Metadata["toolCallId"]!.GetValue<string>()).IsEqualTo("tool-call-1");
        await Assert.That(restoredToolMessage.Metadata["toolName"]!.GetValue<string>()).IsEqualTo("echo");
        await Assert.That(restoredToolMessage.Content).IsEqualTo("Echoed");
        await Assert.That(completedConversation!.Messages).HasSingleItem(x => x.Role == AIMessageRole.User && x.Content == "Use a tool");
        await Assert.That(completedConversation.Messages).HasSingleItem(x => x.Role == AIMessageRole.Tool);
        await Assert.That(completedConversation.Status).IsEqualTo(AIConversationStatus.Completed);
    }

    [Test]
    [DisplayName("Chat orchestration does not load foreign tenant conversation history")]
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

        var request = (await Assert.That(provider.Requests).HasSingleItem())!;
        var original = await store.FindAsync("conversation-1");

        await Assert.That(request.Messages).DoesNotContain(x => x.Content == "Tenant A secret");
        await Assert.That(original!.TenantId).IsEqualTo("tenant-a");
        await Assert.That(original.Messages).HasSingleItem();
    }

    [Test]
    [DisplayName("Chat orchestration does not load foreign user conversation history")]
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

        var request = (await Assert.That(provider.Requests).HasSingleItem())!;
        var original = await store.FindAsync("conversation-1");

        await Assert.That(request.Messages).DoesNotContain(x => x.Content == "User A secret");
        await Assert.That(original!.UserId).IsEqualTo("user-a");
        await Assert.That(original.Messages).HasSingleItem();
    }

    [Test]
    [DisplayName("Chat orchestration limits resolved context payloads")]
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

        var context = (await Assert.That(provider.Requests.Single().Context).HasSingleItem())!;

        await Assert.That(context.Summary.Length).IsEqualTo(64);
        await Assert.That(context.Data["truncated"]!.GetValue<bool>()).IsTrue();
        await Assert.That(context.Data["maxBytes"]!.GetValue<int>()).IsEqualTo(64);
        await Assert.That(context.Metadata["truncated"]!.GetValue<bool>()).IsTrue();
    }

    [Test]
    [DisplayName("Chat orchestration truncates multibyte context to the byte limit")]
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

        var context = (await Assert.That(provider.Requests.Single().Context).HasSingleItem())!;

        await Assert.That(Encoding.UTF8.GetByteCount(context.Summary) <= 64).IsTrue();
        await Assert.That(context.Summary.Length > 16).IsTrue();
    }

    [Test]
    [DisplayName("Chat orchestration does not split surrogate pairs when truncating context")]
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

        var context = (await Assert.That(provider.Requests.Single().Context).HasSingleItem())!;

        await Assert.That(context.Summary.Length > 0 && char.IsHighSurrogate(context.Summary[^1])).IsFalse();
        await Assert.That(Encoding.UTF8.GetByteCount(context.Summary) <= 3).IsTrue();
    }

    [Test]
    [DisplayName("Chat orchestration applies one total resolved context budget")]
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

        await Assert.That(provider.Requests.Single().Context).HasSingleItem();
    }

    [Test]
    [DisplayName("Chat orchestration keeps smaller contexts after an oversized context")]
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

        await Assert.That(contexts).Count().IsEqualTo(2);
        await Assert.That(contexts[0].ReferenceId).IsEqualTo("small-1");
        await Assert.That(contexts[1].ReferenceId).IsEqualTo("small-2");
    }

    [Test]
    [DisplayName("Chat orchestration treats non-positive context byte limit as unlimited")]
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

        var context = (await Assert.That(provider.Requests.Single().Context).HasSingleItem())!;

        await Assert.That(context.Summary.Length).IsEqualTo(512);
        await Assert.That(context.Data.ContainsKey("truncated")).IsFalse();
    }

    [Test]
    [DisplayName("Chat orchestration limits tool result payloads")]
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

        var toolResult = (await Assert.That(events).HasSingleItem(x => x.Type == "tool.result"))!;
        var data = toolResult.Data["data"]!.AsObject();

        await Assert.That(toolResult.Data["summary"]!.GetValue<string>().Length).IsEqualTo(64);
        await Assert.That(data["truncated"]!.GetValue<bool>()).IsTrue();
        await Assert.That(data["maxBytes"]!.GetValue<int>()).IsEqualTo(64);
    }

    [Test]
    [DisplayName("Chat orchestration persists provider-emitted tool results")]
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
        var toolMessage = (await Assert.That(conversation!.Messages).HasSingleItem(x => x.Role == AIMessageRole.Tool))!;

        await Assert.That(toolMessage.Metadata["toolCallId"]!.GetValue<string>()).IsEqualTo("tool-call-1");
        await Assert.That(toolMessage.Metadata["toolName"]!.GetValue<string>()).IsEqualTo("echo");
        await Assert.That(toolMessage.Content).IsEqualTo("Echoed");
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
