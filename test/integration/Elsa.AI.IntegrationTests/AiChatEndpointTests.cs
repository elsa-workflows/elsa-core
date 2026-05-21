using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        Assert.Equal(3, completion.Sequence);
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

        var continuation = Assert.Single(provider.Requests, x => x.ToolResults.Count == 1);
        var toolResult = Assert.Single(continuation.ToolResults);
        var continuationMessages = continuation.Messages.Where(x => x.Role is AiMessageRole.Assistant or AiMessageRole.Tool).ToList();

        Assert.Equal("tool-call-1", toolResult.ToolCallId);
        Assert.Equal("Echoed", toolResult.Result.Summary);
        Assert.Collection(
            continuationMessages,
            assistant => Assert.Equal(AiMessageRole.Assistant, assistant.Role),
            tool => Assert.Equal(AiMessageRole.Tool, tool.Role));
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
        var restoredToolResult = Assert.Single(reconnectRequest.ToolResults);
        var completedConversation = await store.FindAsync("conversation-1");

        Assert.Equal("", reconnectRequest.Message);
        Assert.Equal("tool-call-1", restoredToolResult.ToolCallId);
        Assert.Equal("echo", restoredToolResult.ToolName);
        Assert.Equal("Echoed", restoredToolResult.Result.Summary);
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

            if (request.ToolResults.Count == 0)
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
                    ["content"] = $"Used {request.ToolResults.Single().Result.Summary}"
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

            if (request.ToolResults.Count == 0)
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
                    ["content"] = $"Used {request.ToolResults.Single().Result.Summary}"
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
}
