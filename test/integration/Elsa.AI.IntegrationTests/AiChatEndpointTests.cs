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
                           Message = "Use a tool"
                       }))
            events.Add(streamEvent);

        var toolResult = Assert.Single(events, x => x.Type == "tool.result");
        Assert.Equal("echo", toolResult.Data["toolName"]!.GetValue<string>());
        Assert.Equal(AiToolInvocationStatus.Completed.ToString(), toolResult.Data["status"]!.GetValue<string>());
        Assert.Equal("Echoed", toolResult.Data["summary"]!.GetValue<string>());
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

    private class EchoTool : IAiTool
    {
        public AiToolDefinition Definition { get; } = new()
        {
            Name = "echo",
            DisplayName = "Echo",
            EnabledByDefault = true
        };

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default)
        {
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
