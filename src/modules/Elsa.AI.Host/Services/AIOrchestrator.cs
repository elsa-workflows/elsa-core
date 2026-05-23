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

public class AIOrchestrator(
    IEnumerable<IAIProvider> providers,
    IAIToolRegistry toolRegistry,
    IAIConversationStore conversationStore,
    AIContextResolver contextResolver,
    AIStreamEventMapper streamEventMapper,
    IAIAuditSink auditSink,
    ILogger<AIOrchestrator> logger,
    IOptions<AIHostOptions> options) : IAIOrchestrator
{
    private const int MaxProviderTurns = 8;

    public async IAsyncEnumerable<AIStreamEvent> ExecuteChatAsync(AIChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString("N");
        var sequence = 0L;
        var providerSelection = SelectProvider(request);
        var provider = providerSelection.Provider;
        var conversationPersistenceEnabled = options.Value.ConversationPersistenceEnabled;
        AIConversation? conversation = null;
        Exception? preparationError = null;
        if (conversationPersistenceEnabled)
        {
            try
            {
                conversation = await conversationStore.FindAsync(conversationId, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                preparationError = e;
            }
        }

        if (conversation != null && (!BelongsToTenant(conversation, request.TenantId) || !BelongsToUser(conversation, request.UserId)))
        {
            conversation = null;
            conversationId = Guid.NewGuid().ToString("N");
        }
        var messages = conversation?.Messages.ToList() ?? [];
        if (request.IsReconnect && messages.Count == 0)
            conversationId = Guid.NewGuid().ToString("N");

        if (request.IsReconnect && IsCompletedReconnect(conversation, request.Message))
        {
            var nextSequence = GetNextSequence(messages);
            if (conversation!.Status == AIConversationStatus.Failed)
            {
                var lastAssistantContent = conversation.Messages.LastOrDefault(x => x.Role == AIMessageRole.Assistant)?.Content;
                if (!string.IsNullOrEmpty(lastAssistantContent))
                    yield return CreateEvent("conversation.error", conversationId, nextSequence++, new JsonObject
                    {
                        ["content"] = lastAssistantContent
                    });
            }

            yield return CreateEvent("conversation.completed", conversationId, nextSequence);
            yield break;
        }

        var providerSessionId = conversation?.ProviderSessionId;

        if (preparationError == null && provider != null && string.IsNullOrWhiteSpace(providerSessionId))
        {
            try
            {
                var session = await provider.CreateSessionAsync(new CreateAISessionRequest
                {
                    ConversationId = conversationId,
                    Agent = request.Agent,
                    TenantId = request.TenantId,
                    ProviderConfiguration = providerSelection.Configuration
                }, cancellationToken);
                providerSessionId = session.ProviderSessionId ?? session.Id;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                preparationError = e;
            }
        }

        var isDuplicateReconnectMessage = request.IsReconnect && HasReconnectUserMessage(conversation, request.Message);
        var providerHistory = messages.ToList();
        if (request.IsReconnect && messages.Count > 0)
            sequence = GetNextSequence(messages);

        yield return CreateEvent("conversation.started", conversationId, sequence++);

        var userMessage = isDuplicateReconnectMessage
            ? messages.Last(x => x.Role == AIMessageRole.User && string.Equals(NormalizeMessage(x.Content), NormalizeMessage(request.Message), StringComparison.Ordinal))
            : CreateMessage(conversationId, AIMessageRole.User, request.Message, sequence++);

        if (!isDuplicateReconnectMessage)
            messages.Add(userMessage);

        var knownToolCallIds = RestoreToolResults(messages).Select(x => x.ToolCallId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pendingToolResults = isDuplicateReconnectMessage ? RestorePendingToolResults(messages) : new List<AIToolTurnResult>();

        await TrySaveConversationAsync(conversationId, request, AIConversationStatus.Active, messages, conversation, providerSessionId, cancellationToken);
        await RecordChatAuditAsync("chat.started", request, conversationId, provider?.Name, cancellationToken);

        IReadOnlyCollection<AIResolvedContext> context = [];
        IReadOnlyCollection<AIToolDefinition> tools = [];

        if (preparationError == null)
        {
            try
            {
                context = LimitResolvedContext(await contextResolver.ResolveAsync(request, cancellationToken));
                tools = await toolRegistry.ListAsync(new AIToolQuery
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
        }

        if (preparationError != null)
        {
            const string content = "Weaver could not prepare AI context or tools for this request.";
            logger.LogWarning(preparationError, "Failed to prepare AI chat context or tools for conversation {ConversationId}.", conversationId);
            yield return CreateEvent("conversation.error", conversationId, sequence++, new JsonObject
            {
                ["content"] = content
            });
            messages.Add(CreateMessage(conversationId, AIMessageRole.Assistant, content, sequence - 1));
            await TrySaveConversationAsync(conversationId, request, AIConversationStatus.Failed, messages, conversation, providerSessionId, cancellationToken);
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
            messages.Add(CreateMessage(conversationId, AIMessageRole.Assistant, content, sequence - 1));
        }
        else
        {
            var assistantContent = new StringBuilder();

            for (var turn = 0; turn < MaxProviderTurns; turn++)
            {
                var currentTurnToolResults = new List<AIToolTurnResult>();
                var currentTurnMessages = new List<AIMessage>();
                var currentTurnToolMessages = new List<AIMessage>();
                assistantContent.Clear();

                var turnRequest = new AITurnRequest
                {
                    ConversationId = conversationId,
                    ProviderSessionId = providerSessionId,
                    Message = turn == 0 && !isDuplicateReconnectMessage ? request.Message : "",
                    Messages = providerHistory.ToList(),
                    Context = context,
                    Tools = tools.Where(x => x.IsEnabled).ToList(),
                    ToolResults = GetUnrepresentedToolResults(pendingToolResults, providerHistory),
                    Agent = request.Agent,
                    ProviderConfiguration = providerSelection.Configuration
                };
                Exception? providerTurnError = null;
                await foreach (var providerRead in ReadProviderEventsAsync(provider.ExecuteTurnAsync(turnRequest, cancellationToken), cancellationToken))
                {
                    if (providerRead.Error != null)
                    {
                        providerTurnError = providerRead.Error;
                        break;
                    }

                    var providerEvent = providerRead.Event!;
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
                    var toolMessage = CreateMessage(conversationId, AIMessageRole.Tool, toolExecution.TurnResult.Result.Summary, toolExecution.StreamEvent.Sequence, new JsonObject
                    {
                        ["toolCallId"] = toolExecution.TurnResult.ToolCallId,
                        ["toolName"] = toolExecution.TurnResult.ToolName,
                        ["status"] = toolExecution.TurnResult.Result.Status.ToString()
                    });
                    currentTurnToolMessages.Add(toolMessage);
                }

                if (providerTurnError != null)
                {
                    const string content = "Weaver could not complete the AI provider turn for this request.";
                    logger.LogWarning(providerTurnError, "Failed to execute AI provider turn for conversation {ConversationId}.", conversationId);
                    yield return CreateEvent("conversation.error", conversationId, sequence++, new JsonObject
                    {
                        ["content"] = content
                    });
                    messages.Add(CreateMessage(conversationId, AIMessageRole.Assistant, content, sequence - 1));
                    await TrySaveConversationAsync(conversationId, request, AIConversationStatus.Failed, messages, conversation, providerSessionId, cancellationToken);
                    await RecordChatAuditAsync("chat.failed", request, conversationId, provider.Name, cancellationToken);
                    yield return CreateEvent("conversation.completed", conversationId, sequence);
                    yield break;
                }

                if (assistantContent.Length > 0 || currentTurnToolMessages.Count > 0)
                {
                    var assistantSequence = currentTurnToolMessages.Count > 0
                        ? currentTurnToolMessages.Min(x => x.StreamSequence) - 1
                        : sequence - 1;
                    var assistantMessage = CreateMessage(conversationId, AIMessageRole.Assistant, assistantContent.ToString(), assistantSequence, CreateAssistantToolCallMetadata(currentTurnToolResults));
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
                await TrySaveConversationAsync(conversationId, request, AIConversationStatus.Active, messages, conversation, providerSessionId, cancellationToken);

                if (turn == MaxProviderTurns - 1)
                {
                    const string content = "Tool execution stopped because the provider requested too many continuation turns.";
                    yield return CreateEvent("assistant.delta", conversationId, sequence++, new JsonObject
                    {
                        ["content"] = content
                    });
                    messages.Add(CreateMessage(conversationId, AIMessageRole.Assistant, content, sequence - 1));
                    break;
                }
            }
        }

        await TrySaveConversationAsync(conversationId, request, AIConversationStatus.Completed, messages, conversation, providerSessionId, cancellationToken);
        await RecordChatAuditAsync("chat.completed", request, conversationId, provider?.Name, cancellationToken);
        yield return CreateEvent("conversation.completed", conversationId, sequence);
    }

    private static AIStreamEvent CreateEvent(string type, string conversationId, long sequence, JsonObject? data = null) =>
        new()
        {
            Type = type,
            ConversationId = conversationId,
            Sequence = sequence,
            Timestamp = DateTimeOffset.UtcNow,
            Data = data ?? []
        };

    private static async IAsyncEnumerable<ProviderReadResult> ReadProviderEventsAsync(
        IAsyncEnumerable<AIProviderEvent> providerEvents,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enumerator = providerEvents.GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                AIProviderEvent? providerEvent = null;
                Exception? error = null;
                var hasEvent = false;

                try
                {
                    hasEvent = await enumerator.MoveNextAsync();
                    if (hasEvent)
                        providerEvent = enumerator.Current;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    error = e;
                }

                if (error != null)
                {
                    yield return new ProviderReadResult(null, error);
                    yield break;
                }

                if (!hasEvent)
                    yield break;

                yield return new ProviderReadResult(providerEvent, null);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    private ProviderSelection SelectProvider(AIChatRequest request)
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
        {
            if (availableProviders.Count > 1)
                logger.LogWarning(
                    "Multiple AI providers are available ({ProviderNames}) but no default provider name is configured. Set AIHostOptions.DefaultProviderName to select one.",
                    string.Join(", ", availableProviders.Select(x => x.Name)));

            return new ProviderSelection(null, null);
        }

        var selectedProvider = availableProviders[0];
        var selectedConfiguration = configuredProviders.FirstOrDefault(x => string.Equals(x.Name, selectedProvider.Name, StringComparison.OrdinalIgnoreCase) ||
                                                                            string.Equals(x.Provider, selectedProvider.Name, StringComparison.OrdinalIgnoreCase));

        return new ProviderSelection(selectedProvider, selectedConfiguration?.ToProviderConfiguration());
    }

    private string? FindAgentProviderName(string? agent) =>
        string.IsNullOrWhiteSpace(agent)
            ? null
            : options.Value.Agents.FirstOrDefault(x => string.Equals(x.Name, agent, StringComparison.OrdinalIgnoreCase))?.ProviderName;

    private async ValueTask RecordChatAuditAsync(string type, AIChatRequest request, string conversationId, string? providerName, CancellationToken cancellationToken)
    {
        try
        {
            await auditSink.RecordAsync(new AIAuditEvent
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

    private async ValueTask<ToolExecutionResult> ExecuteToolCallAsync(ToolCall toolCall, AIChatRequest request, string conversationId, long sequence, CancellationToken cancellationToken)
    {
        var tool = await toolRegistry.FindAsync(toolCall.Name, new AIToolQuery
        {
            Agent = request.Agent,
            ActorId = request.UserId,
            TenantId = request.TenantId,
            UserPermissions = request.UserPermissions
        }, cancellationToken);
        if (tool == null)
        {
            var result = new AIToolResult { Status = AIToolInvocationStatus.Failed, Error = $"Tool '{toolCall.Name}' was not found." };
            await RecordToolAuditEventsAsync(request, conversationId, toolCall, ["tool.failed"], cancellationToken);
            return CreateToolExecutionResult(conversationId, sequence, toolCall, result);
        }

        using var toolScope = tool as IDisposable;
        try
        {
            await RecordToolAuditEventsAsync(request, conversationId, toolCall, ["tool.invoked"], cancellationToken);
            var result = await tool.ExecuteAsync(new AIToolExecutionContext
            {
                ConversationId = conversationId,
                TenantId = request.TenantId,
                ActorId = request.UserId,
                Agent = request.Agent,
                Arguments = toolCall.Arguments
            }, cancellationToken);
            await RecordToolAuditEventsAsync(request, conversationId, toolCall, ["tool.completed"], cancellationToken);

            return CreateToolExecutionResult(conversationId, sequence, toolCall, LimitToolResult(result));
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "AI tool {ToolName} failed for conversation {ConversationId}.", toolCall.Name, conversationId);
            await RecordToolAuditEventsAsync(request, conversationId, toolCall, ["tool.failed"], cancellationToken);
            return CreateToolExecutionResult(conversationId, sequence, toolCall, new AIToolResult { Status = AIToolInvocationStatus.Failed, Error = "Tool execution failed." });
        }
    }

    private async ValueTask RecordToolAuditEventsAsync(AIChatRequest request, string conversationId, ToolCall toolCall, IReadOnlyCollection<string> types, CancellationToken cancellationToken)
    {
        try
        {
            await auditSink.RecordManyAsync(types.Select(type => CreateToolAuditEvent(type, request, conversationId, toolCall)).ToList(), cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Failed to record AI tool audit events for tool {ToolName}.", toolCall.Name);
        }
    }

    private static AIAuditEvent CreateToolAuditEvent(string type, AIChatRequest request, string conversationId, ToolCall toolCall) =>
        new()
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
        };

    private static AIStreamEvent CreateToolResultEvent(string conversationId, long sequence, ToolCall toolCall, AIToolResult result) =>
        CreateEvent("tool.result", conversationId, sequence, new JsonObject
        {
            ["toolCallId"] = toolCall.Id,
            ["toolName"] = toolCall.Name,
            ["status"] = result.Status.ToString(),
            ["summary"] = result.Summary,
            ["error"] = result.Error,
            ["data"] = result.Data.DeepClone()
        });

    private static ToolExecutionResult CreateToolExecutionResult(string conversationId, long sequence, ToolCall toolCall, AIToolResult result) =>
        new(CreateToolResultEvent(conversationId, sequence, toolCall, result), new AIToolTurnResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Result = result
        });

    private IReadOnlyCollection<AIResolvedContext> LimitResolvedContext(IReadOnlyCollection<AIResolvedContext> contexts)
    {
        var maxBytes = options.Value.MaxResolvedContextBytes;
        if (maxBytes <= 0)
            return contexts;

        var limited = new List<AIResolvedContext>();
        var usedBytes = 0;

        foreach (var context in contexts)
        {
            var contextSize = GetUtf8Size(context);
            if (usedBytes + contextSize <= maxBytes)
            {
                usedBytes += contextSize;
                limited.Add(context);
                continue;
            }

            if (limited.Count > 0)
            {
                logger.LogDebug(
                    "Dropping AI resolved context {ContextKind}/{ReferenceId} because resolved context exceeds the configured {MaxResolvedContextBytes} byte budget.",
                    context.Kind,
                    context.ReferenceId,
                    maxBytes);
                continue;
            }

            limited.Add(TruncateContext(context, maxBytes));
            break;
        }

        return limited;
    }

    private static AIResolvedContext TruncateContext(AIResolvedContext context, int maxBytes) =>
        context with
        {
            Summary = Truncate(context.Summary, maxBytes),
            Data = CreateTruncatedPayload(maxBytes),
            Metadata = CreateTruncatedPayload(maxBytes)
        };

    private AIToolResult LimitToolResult(AIToolResult result)
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

        if (low > 0 && char.IsHighSurrogate(value[low - 1]))
            low--;

        return value[..low];
    }

    private static bool TryReadToolCall(AIProviderEvent providerEvent, out ToolCall toolCall)
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

    private static bool TryReadAssistantContent(AIProviderEvent providerEvent, out string content)
    {
        content = "";
        if (!string.Equals(providerEvent.Type, "assistant.delta", StringComparison.OrdinalIgnoreCase))
            return false;

        content = providerEvent.Data["content"]?.GetValue<string>() ?? "";
        return !string.IsNullOrEmpty(content);
    }

    private static AIMessage CreateMessage(string conversationId, AIMessageRole role, string content, long streamSequence, JsonObject? metadata = null) =>
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

    private static JsonObject? CreateAssistantToolCallMetadata(IReadOnlyCollection<AIToolTurnResult> toolResults)
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

    private static bool HasReconnectUserMessage(AIConversation? conversation, string message)
    {
        return conversation is { Status: AIConversationStatus.Active } &&
               HasUserMessage(conversation, message);
    }

    private static bool IsCompletedReconnect(AIConversation? conversation, string message) =>
        conversation is { Status: AIConversationStatus.Completed or AIConversationStatus.Failed } && HasUserMessage(conversation, message);

    private static bool HasUserMessage(AIConversation conversation, string message) =>
        conversation.Messages.Any(x => x.Role == AIMessageRole.User && string.Equals(NormalizeMessage(x.Content), NormalizeMessage(message), StringComparison.Ordinal));

    private static long GetNextSequence(IReadOnlyCollection<AIMessage> messages) =>
        messages.Count == 0 ? 0 : messages.Max(x => x.StreamSequence) + 1;

    private static List<AIToolTurnResult> RestoreToolResults(IEnumerable<AIMessage> messages)
    {
        return messages
            .Where(x => x.Role == AIMessageRole.Tool)
            .Select(CreateToolTurnResult)
            .OfType<AIToolTurnResult>()
            .ToList();
    }

    private static List<AIToolTurnResult> RestorePendingToolResults(IReadOnlyCollection<AIMessage> messages)
    {
        return messages
            .Reverse()
            .TakeWhile(x => x.Role == AIMessageRole.Tool)
            .Reverse()
            .Select(CreateToolTurnResult)
            .OfType<AIToolTurnResult>()
            .ToList();
    }

    private static IReadOnlyCollection<AIToolTurnResult> GetUnrepresentedToolResults(IReadOnlyCollection<AIToolTurnResult> toolResults, IReadOnlyCollection<AIMessage> messages)
    {
        if (toolResults.Count == 0)
            return [];

        var representedToolCallIds = messages
            .Where(x => x.Role == AIMessageRole.Tool)
            .Select(x => x.Metadata["toolCallId"]?.GetValue<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return toolResults
            .Where(x => !representedToolCallIds.Contains(x.ToolCallId))
            .ToList();
    }

    private static AIToolTurnResult? CreateToolTurnResult(AIMessage message)
    {
        var toolCallId = message.Metadata["toolCallId"]?.GetValue<string>();
        var toolName = message.Metadata["toolName"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(toolCallId) || string.IsNullOrWhiteSpace(toolName))
            return null;

        var status = Enum.TryParse<AIToolInvocationStatus>(message.Metadata["status"]?.GetValue<string>(), out var parsedStatus)
            ? parsedStatus
            : AIToolInvocationStatus.Completed;

        return new AIToolTurnResult
        {
            ToolCallId = toolCallId,
            ToolName = toolName,
            Result = new AIToolResult
            {
                Status = status,
                Summary = message.Content
            }
        };
    }

    private static string NormalizeMessage(string message) =>
        message.ReplaceLineEndings("\n").Trim();

    private static bool BelongsToTenant(AIConversation conversation, string? tenantId) =>
        string.Equals(NormalizeTenantId(conversation.TenantId), NormalizeTenantId(tenantId), StringComparison.Ordinal);

    private static bool BelongsToUser(AIConversation conversation, string userId) =>
        string.IsNullOrWhiteSpace(conversation.UserId) || string.Equals(conversation.UserId, userId, StringComparison.Ordinal);

    private static string NormalizeTenantId(string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? "" : tenantId;

    private async ValueTask TrySaveConversationAsync(string conversationId, AIChatRequest request, AIConversationStatus status, IReadOnlyCollection<AIMessage> messages, AIConversation? conversation, string? providerSessionId, CancellationToken cancellationToken)
    {
        if (!options.Value.ConversationPersistenceEnabled)
            return;

        try
        {
            var now = DateTimeOffset.UtcNow;
            var retentionMode = conversation?.RetentionMode ?? AIRetentionMode.Configured;
            var retentionExpiresAt = conversation?.RetentionExpiresAt ?? (retentionMode == AIRetentionMode.Configured ? now.Add(options.Value.ConversationRetention) : null);

            await conversationStore.SaveAsync(new AIConversation
            {
                Id = conversationId,
                TenantId = request.TenantId,
                UserId = request.UserId,
                Title = conversation?.Title,
                Status = status,
                CreatedAt = conversation is null || conversation.CreatedAt == default ? now : conversation.CreatedAt,
                UpdatedAt = now,
                ProviderSessionId = providerSessionId ?? conversation?.ProviderSessionId,
                RetentionMode = retentionMode,
                RetentionExpiresAt = retentionExpiresAt,
                Messages = messages.ToList()
            }, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Failed to persist AI conversation {ConversationId} with status {ConversationStatus}.", conversationId, status);
        }
    }

    private readonly record struct ToolCall(string Id, string Name, JsonObject Arguments);
    private readonly record struct ToolExecutionResult(AIStreamEvent StreamEvent, AIToolTurnResult TurnResult);
    private readonly record struct ProviderReadResult(AIProviderEvent? Event, Exception? Error);
    private readonly record struct ProviderSelection(IAIProvider? Provider, AIProviderConfiguration? Configuration);
}
