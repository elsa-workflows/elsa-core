using System.Text.Json;
using Elsa.AI.Abstractions.Models;
using GitHub.Copilot;

namespace Elsa.AI.Copilot.Adapters;

public class CopilotSessionEventMapper
{
    public IEnumerable<AIProviderEvent> Map(SessionEvent sessionEvent)
    {
        var timestamp = sessionEvent.Timestamp == default ? DateTimeOffset.UtcNow : sessionEvent.Timestamp;
        var sequence = 0L;

        switch (sessionEvent)
        {
            case AssistantMessageDeltaEvent assistantDelta when !string.IsNullOrEmpty(assistantDelta.Data?.DeltaContent):
                yield return Create("assistant.delta", sequence++, timestamp, new JsonObject
                {
                    ["content"] = assistantDelta.Data.DeltaContent,
                    ["messageId"] = assistantDelta.Data.MessageId,
                    ["parentToolCallId"] = assistantDelta.Data.ParentToolCallId
                });
                break;
            case AssistantMessageEvent assistantMessage when !string.IsNullOrEmpty(assistantMessage.Data?.Content):
                yield return Create("assistant.message", sequence++, timestamp, new JsonObject
                {
                    ["content"] = assistantMessage.Data.Content,
                    ["messageId"] = assistantMessage.Data.MessageId,
                    ["model"] = assistantMessage.Data.Model,
                    ["turnId"] = assistantMessage.Data.TurnId
                });
                break;
            case AssistantReasoningDeltaEvent reasoningDelta when !string.IsNullOrEmpty(reasoningDelta.Data?.DeltaContent):
                yield return Create("assistant.reasoning.delta", sequence++, timestamp, new JsonObject
                {
                    ["content"] = reasoningDelta.Data.DeltaContent,
                    ["reasoningId"] = reasoningDelta.Data.ReasoningId
                });
                break;
            case AssistantReasoningEvent reasoning when !string.IsNullOrEmpty(reasoning.Data?.Content):
                yield return Create("assistant.reasoning", sequence++, timestamp, new JsonObject
                {
                    ["content"] = reasoning.Data.Content,
                    ["reasoningId"] = reasoning.Data.ReasoningId
                });
                break;
            case ToolExecutionStartEvent toolStart:
                yield return Create("tool.started", sequence++, timestamp, new JsonObject
                {
                    ["toolCallId"] = toolStart.Data?.ToolCallId,
                    ["toolName"] = ReadToolName(toolStart.Data?.ToolName, toolStart.Data?.McpToolName),
                    ["mcpServerName"] = toolStart.Data?.McpServerName,
                    ["arguments"] = toolStart.Data?.Arguments is { } arguments ? JsonNode.Parse(arguments.GetRawText()) : null
                });
                break;
            case ToolExecutionCompleteEvent toolComplete:
                yield return Create("tool.result", sequence++, timestamp, new JsonObject
                {
                    ["toolCallId"] = toolComplete.Data?.ToolCallId,
                    ["toolName"] = ReadToolName(toolComplete.Data?.ToolDescription?.Name, null),
                    ["status"] = toolComplete.Data?.Success == false ? AIToolInvocationStatus.Failed.ToString() : AIToolInvocationStatus.Completed.ToString(),
                    ["summary"] = toolComplete.Data?.Result?.Content ?? "",
                    ["error"] = toolComplete.Data?.Error?.Message
                });
                break;
            case SessionErrorEvent error:
                yield return Create("conversation.error", sequence++, timestamp, new JsonObject
                {
                    ["content"] = error.Data?.Message ?? "Copilot session error.",
                    ["errorCode"] = error.Data?.ErrorCode,
                    ["errorType"] = error.Data?.ErrorType,
                    ["statusCode"] = error.Data?.StatusCode
                });
                break;
        }
    }

    private static AIProviderEvent Create(string type, long sequence, DateTimeOffset timestamp, JsonObject data) =>
        new()
        {
            Type = type,
            Sequence = sequence,
            Timestamp = timestamp,
            Data = data
        };

    private static string? ReadToolName(string? toolName, string? mcpToolName) =>
        !string.IsNullOrWhiteSpace(toolName) ? toolName : mcpToolName;
}
