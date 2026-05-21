using System.Text;
using System.Text.Json;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Context;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Streaming;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Services;

public class AiOrchestrator(
    IEnumerable<IAiProvider> providers,
    IAiToolRegistry toolRegistry,
    IAiConversationStore conversationStore,
    AiContextResolver contextResolver,
    AiStreamEventMapper streamEventMapper,
    IAiAuditSink auditSink,
    ILogger<AiOrchestrator> logger,
    IOptions<AiHostOptions> options) : IAiOrchestrator
{
    private const int MaxProviderTurns = 8;

    public async IAsyncEnumerable<AiStreamEvent> ExecuteChatAsync(AiChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString("N");
        var sequence = 0L;
        var provider = SelectProvider(request);
        var conversation = await conversationStore.FindAsync(conversationId, cancellationToken);
        var messages = conversation?.Messages.ToList() ?? [];
        messages.Add(CreateMessage(conversationId, AiMessageRole.User, request.Message, sequence));
        var toolResults = new List<AiToolTurnResult>();

        yield return CreateEvent("conversation.started", conversationId, sequence++);
        await SaveConversationAsync(conversationId, request, AiConversationStatus.Active, messages, cancellationToken);
        await RecordChatAuditAsync("chat.started", request, conversationId, provider?.Name, cancellationToken);

        var context = LimitResolvedContext(await contextResolver.ResolveAsync(request, cancellationToken));
        var tools = await toolRegistry.ListAsync(new AiToolQuery
        {
            Agent = request.Agent,
            ActorId = request.UserId,
            TenantId = request.TenantId,
            UserPermissions = request.UserPermissions
        }, cancellationToken);

        if (provider == null)
        {
            const string content = "Weaver is ready, but no AI provider is configured.";
            yield return CreateEvent("assistant.delta", conversationId, sequence++, new JsonObject
            {
                ["content"] = content
            });
            messages.Add(CreateMessage(conversationId, AiMessageRole.Assistant, content, sequence - 1));
        }
        else
        {
            for (var turn = 0; turn < MaxProviderTurns; turn++)
            {
                var currentTurnToolResults = new List<AiToolTurnResult>();
                var assistantContent = new StringBuilder();

                await foreach (var providerEvent in provider.ExecuteTurnAsync(new AiTurnRequest
                               {
                                   ConversationId = conversationId,
                                   Message = request.Message,
                                   Messages = messages.ToList(),
                                   Context = context,
                                   Tools = tools,
                                   ToolResults = toolResults.ToList(),
                                   Agent = request.Agent
                               }, cancellationToken))
                {
                    var streamEvent = streamEventMapper.Map(conversationId, providerEvent) with { Sequence = sequence++ };
                    yield return streamEvent;

                    if (TryReadAssistantContent(providerEvent, out var content))
                        assistantContent.Append(content);

                    if (!TryReadToolCall(providerEvent, out var toolCall))
                        continue;

                    if (toolResults.Any(x => string.Equals(x.ToolCallId, toolCall.Id, StringComparison.OrdinalIgnoreCase)) ||
                        currentTurnToolResults.Any(x => string.Equals(x.ToolCallId, toolCall.Id, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var toolExecution = await ExecuteToolCallAsync(toolCall, request, conversationId, sequence++, cancellationToken);
                    yield return toolExecution.StreamEvent;

                    currentTurnToolResults.Add(toolExecution.TurnResult);
                    messages.Add(CreateMessage(conversationId, AiMessageRole.Tool, toolExecution.TurnResult.Result.Summary, toolExecution.StreamEvent.Sequence, new JsonObject
                    {
                        ["toolCallId"] = toolExecution.TurnResult.ToolCallId,
                        ["toolName"] = toolExecution.TurnResult.ToolName,
                        ["status"] = toolExecution.TurnResult.Result.Status.ToString()
                    }));
                }

                if (assistantContent.Length > 0)
                    messages.Add(CreateMessage(conversationId, AiMessageRole.Assistant, assistantContent.ToString(), sequence - 1));

                if (currentTurnToolResults.Count == 0)
                    break;

                toolResults.AddRange(currentTurnToolResults);

                if (turn == MaxProviderTurns - 1)
                    yield return CreateEvent("assistant.delta", conversationId, sequence++, new JsonObject
                    {
                        ["content"] = "Tool execution stopped because the provider requested too many continuation turns."
                    });
            }
        }

        await SaveConversationAsync(conversationId, request, AiConversationStatus.Completed, messages, cancellationToken);
        await RecordChatAuditAsync("chat.completed", request, conversationId, provider?.Name, cancellationToken);
        yield return CreateEvent("conversation.completed", conversationId, sequence);
    }

    private static AiStreamEvent CreateEvent(string type, string conversationId, long sequence, JsonObject? data = null) =>
        new()
        {
            Type = type,
            ConversationId = conversationId,
            Sequence = sequence,
            Timestamp = DateTimeOffset.UtcNow,
            Data = data ?? []
        };

    private IAiProvider? SelectProvider(AiChatRequest request)
    {
        var availableProviders = providers.ToList();
        var providerName = request.ProviderName ?? FindAgentProviderName(request.Agent) ?? options.Value.DefaultProviderName;

        if (!string.IsNullOrWhiteSpace(providerName))
            return availableProviders.FirstOrDefault(x => string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase));

        return availableProviders.Count == 1 ? availableProviders[0] : null;
    }

    private string? FindAgentProviderName(string? agent) =>
        string.IsNullOrWhiteSpace(agent)
            ? null
            : options.Value.Agents.FirstOrDefault(x => string.Equals(x.Name, agent, StringComparison.OrdinalIgnoreCase))?.ProviderName;

    private async ValueTask RecordChatAuditAsync(string type, AiChatRequest request, string conversationId, string? providerName, CancellationToken cancellationToken)
    {
        try
        {
            await auditSink.RecordAsync(new AiAuditEvent
            {
                Type = type,
                TenantId = request.TenantId,
                ActorId = request.UserId,
                ConversationId = conversationId,
                Timestamp = DateTimeOffset.UtcNow,
                Summary = type == "chat.started" ? "Chat started" : "Chat completed",
                Data = new JsonObject
                {
                    ["agent"] = request.Agent,
                    ["provider"] = providerName,
                    ["attachmentCount"] = request.Attachments.Count
                }
            }, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to record AI chat audit event {AuditEventType} for conversation {ConversationId}.", type, conversationId);
        }
    }

    private async ValueTask<ToolExecutionResult> ExecuteToolCallAsync(ToolCall toolCall, AiChatRequest request, string conversationId, long sequence, CancellationToken cancellationToken)
    {
        var tool = await toolRegistry.FindAsync(toolCall.Name, new AiToolQuery
        {
            Agent = request.Agent,
            ActorId = request.UserId,
            TenantId = request.TenantId,
            UserPermissions = request.UserPermissions
        }, cancellationToken);
        if (tool == null)
        {
            var result = new AiToolResult { Status = AiToolInvocationStatus.Failed, Error = $"Tool '{toolCall.Name}' was not found." };
            return CreateToolExecutionResult(conversationId, sequence, toolCall, result);
        }

        try
        {
            await RecordToolAuditAsync("tool.invoked", request, conversationId, toolCall, cancellationToken);
            var result = await tool.ExecuteAsync(new AiToolExecutionContext
            {
                ConversationId = conversationId,
                TenantId = request.TenantId,
                ActorId = request.UserId,
                Agent = request.Agent,
                Arguments = toolCall.Arguments
            }, cancellationToken);
            await RecordToolAuditAsync("tool.completed", request, conversationId, toolCall, cancellationToken);

            return CreateToolExecutionResult(conversationId, sequence, toolCall, LimitToolResult(result));
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "AI tool {ToolName} failed for conversation {ConversationId}.", toolCall.Name, conversationId);
            await RecordToolAuditAsync("tool.failed", request, conversationId, toolCall, cancellationToken);
            return CreateToolExecutionResult(conversationId, sequence, toolCall, new AiToolResult { Status = AiToolInvocationStatus.Failed, Error = e.Message });
        }
    }

    private async ValueTask RecordToolAuditAsync(string type, AiChatRequest request, string conversationId, ToolCall toolCall, CancellationToken cancellationToken)
    {
        try
        {
            await auditSink.RecordAsync(new AiAuditEvent
            {
                Type = type,
                TenantId = request.TenantId,
                ActorId = request.UserId,
                ConversationId = conversationId,
                ToolInvocationId = toolCall.Id,
                Timestamp = DateTimeOffset.UtcNow,
                Summary = $"{toolCall.Name} {type}",
                Data = new JsonObject
                {
                    ["toolName"] = toolCall.Name
                }
            }, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to record AI tool audit event {AuditEventType} for tool {ToolName}.", type, toolCall.Name);
        }
    }

    private static AiStreamEvent CreateToolResultEvent(string conversationId, long sequence, ToolCall toolCall, AiToolResult result) =>
        CreateEvent("tool.result", conversationId, sequence, new JsonObject
        {
            ["toolCallId"] = toolCall.Id,
            ["toolName"] = toolCall.Name,
            ["status"] = result.Status.ToString(),
            ["summary"] = result.Summary,
            ["error"] = result.Error,
            ["data"] = result.Data.DeepClone()
        });

    private static ToolExecutionResult CreateToolExecutionResult(string conversationId, long sequence, ToolCall toolCall, AiToolResult result) =>
        new(CreateToolResultEvent(conversationId, sequence, toolCall, result), new AiToolTurnResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Result = result
        });

    private IReadOnlyCollection<AiResolvedContext> LimitResolvedContext(IReadOnlyCollection<AiResolvedContext> contexts) =>
        contexts.Select(context =>
        {
            if (GetUtf8Size(context) <= options.Value.MaxResolvedContextBytes)
                return context;

            return context with
            {
                Summary = Truncate(context.Summary, options.Value.MaxResolvedContextBytes),
                Data = CreateTruncatedPayload(options.Value.MaxResolvedContextBytes),
                Metadata = CreateTruncatedPayload(options.Value.MaxResolvedContextBytes)
            };
        }).ToList();

    private AiToolResult LimitToolResult(AiToolResult result)
    {
        if (GetUtf8Size(result) <= options.Value.MaxToolResultBytes)
            return result;

        return result with
        {
            Summary = Truncate(result.Summary, options.Value.MaxToolResultBytes),
            Data = CreateTruncatedPayload(options.Value.MaxToolResultBytes)
        };
    }

    private static JsonObject CreateTruncatedPayload(int maxBytes) =>
        new()
        {
            ["truncated"] = true,
            ["maxBytes"] = maxBytes
        };

    private static int GetUtf8Size<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value).Length;

    private static string Truncate(string value, int maxBytes)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var maxChars = Math.Min(value.Length, Math.Max(0, maxBytes / 4));
        var truncated = value[..maxChars];

        while (Encoding.UTF8.GetByteCount(truncated) > maxBytes && truncated.Length > 0)
            truncated = truncated[..^1];

        return truncated;
    }

    private static bool TryReadToolCall(AiProviderEvent providerEvent, out ToolCall toolCall)
    {
        toolCall = default;
        if (!string.Equals(providerEvent.Type, "tool.call", StringComparison.OrdinalIgnoreCase))
            return false;

        var name = providerEvent.Data["toolName"]?.GetValue<string>() ?? providerEvent.Data["name"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var id = providerEvent.Data["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N");
        var arguments = providerEvent.Data["arguments"]?.DeepClone() as JsonObject ?? [];
        toolCall = new ToolCall(id, name, arguments);
        return true;
    }

    private static bool TryReadAssistantContent(AiProviderEvent providerEvent, out string content)
    {
        content = "";
        if (!string.Equals(providerEvent.Type, "assistant.delta", StringComparison.OrdinalIgnoreCase))
            return false;

        content = providerEvent.Data["content"]?.GetValue<string>() ?? "";
        return !string.IsNullOrEmpty(content);
    }

    private static AiMessage CreateMessage(string conversationId, AiMessageRole role, string content, long streamSequence, JsonObject? metadata = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
            StreamSequence = streamSequence,
            Metadata = metadata ?? []
        };

    private async ValueTask SaveConversationAsync(string conversationId, AiChatRequest request, AiConversationStatus status, IReadOnlyCollection<AiMessage> messages, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var conversation = await conversationStore.FindAsync(conversationId, cancellationToken);

        await conversationStore.SaveAsync(new AiConversation
        {
            Id = conversationId,
            TenantId = request.TenantId,
            UserId = request.UserId,
            Status = status,
            CreatedAt = conversation is null || conversation.CreatedAt == default ? now : conversation.CreatedAt,
            UpdatedAt = now,
            RetentionMode = conversation?.RetentionMode ?? AiRetentionMode.Configured,
            RetentionExpiresAt = conversation?.RetentionExpiresAt,
            Messages = messages.ToList()
        }, cancellationToken);
    }

    private readonly record struct ToolCall(string Id, string Name, JsonObject Arguments);
    private readonly record struct ToolExecutionResult(AiStreamEvent StreamEvent, AiToolTurnResult TurnResult);
}
