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
        var providerSelection = SelectProvider(request);
        var provider = providerSelection.Provider;
        var conversation = await conversationStore.FindAsync(conversationId, cancellationToken);
        if (conversation != null && (!BelongsToTenant(conversation, request.TenantId) || !BelongsToUser(conversation, request.UserId)))
        {
            conversation = null;
            conversationId = Guid.NewGuid().ToString("N");
        }
        var messages = conversation?.Messages.ToList() ?? [];

        if (request.IsReconnect && IsCompletedReconnect(conversation, request.Message))
        {
            yield return CreateEvent("conversation.completed", conversationId, GetNextSequence(messages));
            yield break;
        }

        var providerSessionId = conversation?.ProviderSessionId;

        if (provider != null && string.IsNullOrWhiteSpace(providerSessionId))
        {
            var session = await provider.CreateSessionAsync(new CreateAiSessionRequest
            {
                ConversationId = conversationId,
                Agent = request.Agent,
                TenantId = request.TenantId,
                ProviderConfiguration = providerSelection.Configuration
            }, cancellationToken);
            providerSessionId = session.ProviderSessionId ?? session.Id;
        }

        var isDuplicateReconnectMessage = request.IsReconnect && HasReconnectUserMessage(conversation, request.Message);
        var providerHistory = messages.ToList();
        yield return CreateEvent("conversation.started", conversationId, sequence++);

        var userMessage = isDuplicateReconnectMessage
            ? messages.Last(x => x.Role == AiMessageRole.User && string.Equals(NormalizeMessage(x.Content), NormalizeMessage(request.Message), StringComparison.Ordinal))
            : CreateMessage(conversationId, AiMessageRole.User, request.Message, sequence);

        if (!isDuplicateReconnectMessage)
            messages.Add(userMessage);

        var knownToolCallIds = RestoreToolResults(messages).Select(x => x.ToolCallId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pendingToolResults = isDuplicateReconnectMessage ? RestorePendingToolResults(messages) : new List<AiToolTurnResult>();

        await SaveConversationAsync(conversationId, request, AiConversationStatus.Active, messages, conversation, providerSessionId, cancellationToken);
        await RecordChatAuditAsync("chat.started", request, conversationId, provider?.Name, cancellationToken);

        IReadOnlyCollection<AiResolvedContext> context = [];
        IReadOnlyCollection<AiToolDefinition> tools = [];
        Exception? preparationError = null;

        try
        {
            context = LimitResolvedContext(await contextResolver.ResolveAsync(request, cancellationToken));
            tools = await toolRegistry.ListAsync(new AiToolQuery
            {
                Agent = request.Agent,
                ActorId = request.UserId,
                TenantId = request.TenantId,
                UserPermissions = request.UserPermissions
            }, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            preparationError = e;
        }

        if (preparationError != null)
        {
            const string content = "Weaver could not prepare AI context or tools for this request.";
            logger.LogWarning(preparationError, "Failed to prepare AI chat context or tools for conversation {ConversationId}.", conversationId);
            yield return CreateEvent("conversation.error", conversationId, sequence++, new JsonObject
            {
                ["content"] = content
            });
            messages.Add(CreateMessage(conversationId, AiMessageRole.Assistant, content, sequence - 1));
            await SaveConversationAsync(conversationId, request, AiConversationStatus.Failed, messages, conversation, providerSessionId, cancellationToken);
            await RecordChatAuditAsync("chat.failed", request, conversationId, provider?.Name, cancellationToken);
            yield return CreateEvent("conversation.completed", conversationId, sequence);
            yield break;
        }

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
            var assistantContent = new StringBuilder();

            for (var turn = 0; turn < MaxProviderTurns; turn++)
            {
                var currentTurnToolResults = new List<AiToolTurnResult>();
                var currentTurnMessages = new List<AiMessage>();
                var currentTurnToolMessages = new List<AiMessage>();
                assistantContent.Clear();

                await foreach (var providerEvent in provider.ExecuteTurnAsync(new AiTurnRequest
                               {
                                   ConversationId = conversationId,
                                   Message = turn == 0 && !isDuplicateReconnectMessage ? request.Message : "",
                                   Messages = providerHistory.ToList(),
                                   Context = context,
                                   Tools = tools,
                                   ToolResults = pendingToolResults.ToList(),
                                   Agent = request.Agent,
                                   ProviderConfiguration = providerSelection.Configuration
                               }, cancellationToken))
                {
                    var streamEvent = streamEventMapper.Map(conversationId, providerEvent) with { Sequence = sequence++ };
                    yield return streamEvent;

                    if (TryReadAssistantContent(providerEvent, out var content))
                        assistantContent.Append(content);

                    if (!TryReadToolCall(providerEvent, out var toolCall))
                        continue;

                    if (knownToolCallIds.Contains(toolCall.Id) ||
                        currentTurnToolResults.Any(x => string.Equals(x.ToolCallId, toolCall.Id, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var toolExecution = await ExecuteToolCallAsync(toolCall, request, conversationId, sequence++, cancellationToken);
                    yield return toolExecution.StreamEvent;

                    currentTurnToolResults.Add(toolExecution.TurnResult);
                    var toolMessage = CreateMessage(conversationId, AiMessageRole.Tool, toolExecution.TurnResult.Result.Summary, toolExecution.StreamEvent.Sequence, new JsonObject
                    {
                        ["toolCallId"] = toolExecution.TurnResult.ToolCallId,
                        ["toolName"] = toolExecution.TurnResult.ToolName,
                        ["status"] = toolExecution.TurnResult.Result.Status.ToString()
                    });
                    currentTurnToolMessages.Add(toolMessage);
                }

                if (assistantContent.Length > 0 || currentTurnToolMessages.Count > 0)
                {
                    var assistantSequence = currentTurnToolMessages.Count > 0
                        ? currentTurnToolMessages.Min(x => x.StreamSequence) - 1
                        : sequence - 1;
                    var assistantMessage = CreateMessage(conversationId, AiMessageRole.Assistant, assistantContent.ToString(), assistantSequence, CreateAssistantToolCallMetadata(currentTurnToolResults));
                    messages.Add(assistantMessage);
                    currentTurnMessages.Add(assistantMessage);
                }

                messages.AddRange(currentTurnToolMessages);
                currentTurnMessages.AddRange(currentTurnToolMessages);

                if (currentTurnToolResults.Count == 0)
                    break;

                foreach (var toolResult in currentTurnToolResults)
                    knownToolCallIds.Add(toolResult.ToolCallId);

                pendingToolResults = currentTurnToolResults;
                if (providerHistory.All(x => x.Id != userMessage.Id))
                    providerHistory.Add(userMessage);

                providerHistory.AddRange(currentTurnMessages);
                await SaveConversationAsync(conversationId, request, AiConversationStatus.Active, messages, conversation, providerSessionId, cancellationToken);

                if (turn == MaxProviderTurns - 1)
                    yield return CreateEvent("assistant.delta", conversationId, sequence++, new JsonObject
                    {
                        ["content"] = "Tool execution stopped because the provider requested too many continuation turns."
                    });
            }
        }

        await SaveConversationAsync(conversationId, request, AiConversationStatus.Completed, messages, conversation, providerSessionId, cancellationToken);
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

    private ProviderSelection SelectProvider(AiChatRequest request)
    {
        var providerOptions = options.Value.Providers.ToList();
        var configuredProviders = providerOptions.Where(x => x.Enabled).ToList();
        var availableProviders = providers
            .Where(x => providerOptions.IsProviderEnabled(x.Name))
            .ToList();
        var providerName = request.ProviderName ?? FindAgentProviderName(request.Agent) ?? options.Value.DefaultProviderName;

        if (!string.IsNullOrWhiteSpace(providerName))
        {
            var configuredProvider = configuredProviders.FirstOrDefault(x => string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase));
            var provider = configuredProvider != null
                ? availableProviders.FirstOrDefault(x => string.Equals(x.Name, configuredProvider.Name, StringComparison.OrdinalIgnoreCase)) ??
                  availableProviders.FirstOrDefault(x => string.Equals(x.Name, configuredProvider.Provider, StringComparison.OrdinalIgnoreCase))
                : availableProviders.FirstOrDefault(x => string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase));

            return new ProviderSelection(provider, configuredProvider?.ToProviderConfiguration());
        }

        if (availableProviders.Count != 1)
            return new ProviderSelection(null, null);

        var selectedProvider = availableProviders[0];
        var selectedConfiguration = configuredProviders.FirstOrDefault(x => string.Equals(x.Name, selectedProvider.Name, StringComparison.OrdinalIgnoreCase) ||
                                                                            string.Equals(x.Provider, selectedProvider.Name, StringComparison.OrdinalIgnoreCase));

        return new ProviderSelection(selectedProvider, selectedConfiguration?.ToProviderConfiguration());
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
                Summary = type switch
                {
                    "chat.started" => "Chat started",
                    "chat.failed" => "Chat failed",
                    _ => "Chat completed"
                },
                Data = new JsonObject
                {
                    ["agent"] = request.Agent,
                    ["provider"] = providerName,
                    ["attachmentCount"] = request.Attachments.Count
                }
            }, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
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
        catch (Exception e) when (e is not OperationCanceledException)
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
        catch (Exception e) when (e is not OperationCanceledException)
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

        if (maxBytes <= 0)
            return "";

        if (Encoding.UTF8.GetByteCount(value) <= maxBytes)
            return value;

        var low = 0;
        var high = Math.Min(value.Length, maxBytes);
        while (low < high)
        {
            var candidate = (low + high + 1) / 2;
            if (Encoding.UTF8.GetByteCount(value.AsSpan(0, candidate)) <= maxBytes)
                low = candidate;
            else
                high = candidate - 1;
        }

        return value[..low];
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

    private static JsonObject? CreateAssistantToolCallMetadata(IReadOnlyCollection<AiToolTurnResult> toolResults)
    {
        if (toolResults.Count == 0)
            return null;

        var toolCallIds = new JsonArray();
        foreach (var toolResult in toolResults)
            toolCallIds.Add(toolResult.ToolCallId);

        return new JsonObject
        {
            ["toolCallIds"] = toolCallIds
        };
    }

    private static bool HasReconnectUserMessage(AiConversation? conversation, string message)
    {
        return conversation is { Status: AiConversationStatus.Active } &&
               HasUserMessage(conversation, message);
    }

    private static bool IsCompletedReconnect(AiConversation? conversation, string message) =>
        conversation is { Status: AiConversationStatus.Completed } && HasUserMessage(conversation, message);

    private static bool HasUserMessage(AiConversation conversation, string message) =>
        conversation.Messages.Any(x => x.Role == AiMessageRole.User && string.Equals(NormalizeMessage(x.Content), NormalizeMessage(message), StringComparison.Ordinal));

    private static long GetNextSequence(IReadOnlyCollection<AiMessage> messages) =>
        messages.Count == 0 ? 0 : messages.Max(x => x.StreamSequence) + 1;

    private static List<AiToolTurnResult> RestoreToolResults(IEnumerable<AiMessage> messages)
    {
        return messages
            .Where(x => x.Role == AiMessageRole.Tool)
            .Select(CreateToolTurnResult)
            .OfType<AiToolTurnResult>()
            .ToList();
    }

    private static List<AiToolTurnResult> RestorePendingToolResults(IReadOnlyCollection<AiMessage> messages)
    {
        return messages
            .Reverse()
            .TakeWhile(x => x.Role == AiMessageRole.Tool)
            .Reverse()
            .Select(CreateToolTurnResult)
            .OfType<AiToolTurnResult>()
            .ToList();
    }

    private static AiToolTurnResult? CreateToolTurnResult(AiMessage message)
    {
        var toolCallId = message.Metadata["toolCallId"]?.GetValue<string>();
        var toolName = message.Metadata["toolName"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(toolCallId) || string.IsNullOrWhiteSpace(toolName))
            return null;

        var status = Enum.TryParse<AiToolInvocationStatus>(message.Metadata["status"]?.GetValue<string>(), out var parsedStatus)
            ? parsedStatus
            : AiToolInvocationStatus.Completed;

        return new AiToolTurnResult
        {
            ToolCallId = toolCallId,
            ToolName = toolName,
            Result = new AiToolResult
            {
                Status = status,
                Summary = message.Content
            }
        };
    }

    private static string NormalizeMessage(string message) =>
        message.ReplaceLineEndings("\n").Trim();

    private static bool BelongsToTenant(AiConversation conversation, string? tenantId) =>
        string.Equals(NormalizeTenantId(conversation.TenantId), NormalizeTenantId(tenantId), StringComparison.Ordinal);

    private static bool BelongsToUser(AiConversation conversation, string userId) =>
        string.IsNullOrWhiteSpace(conversation.UserId) || string.Equals(conversation.UserId, userId, StringComparison.Ordinal);

    private static string NormalizeTenantId(string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? "" : tenantId;

    private async ValueTask SaveConversationAsync(string conversationId, AiChatRequest request, AiConversationStatus status, IReadOnlyCollection<AiMessage> messages, AiConversation? conversation, string? providerSessionId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var retentionMode = conversation?.RetentionMode ?? AiRetentionMode.Configured;
        var retentionExpiresAt = conversation?.RetentionExpiresAt ?? (retentionMode == AiRetentionMode.Configured ? now.Add(options.Value.ConversationRetention) : null);

        await conversationStore.SaveAsync(new AiConversation
        {
            Id = conversationId,
            TenantId = request.TenantId,
            UserId = request.UserId,
            Status = status,
            CreatedAt = conversation is null || conversation.CreatedAt == default ? now : conversation.CreatedAt,
            UpdatedAt = now,
            ProviderSessionId = providerSessionId ?? conversation?.ProviderSessionId,
            RetentionMode = retentionMode,
            RetentionExpiresAt = retentionExpiresAt,
            Messages = messages.ToList()
        }, cancellationToken);
    }

    private readonly record struct ToolCall(string Id, string Name, JsonObject Arguments);
    private readonly record struct ToolExecutionResult(AiStreamEvent StreamEvent, AiToolTurnResult TurnResult);
    private readonly record struct ProviderSelection(IAiProvider? Provider, AiProviderConfiguration? Configuration);
}
