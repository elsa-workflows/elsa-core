using Elsa.Studio.AI.Models;
using System.Text.Json.Nodes;

namespace Elsa.Studio.AI.Services;

/// <summary>
/// Applies provider-neutral Weaver stream events to UI state.
/// </summary>
public static class WeaverWorkspaceReducer
{
    public static void AddUserTurn(WeaverWorkspaceState state, string message, DateTimeOffset timestamp)
    {
        state.Messages.Add(new(Guid.NewGuid().ToString("N"), WeaverMessageRole.User, message, timestamp, state.Messages.Count == 0 ? 0 : state.Messages.Max(x => x.Sequence) + 1));
        state.IsBusy = true;
        state.ErrorMessage = null;
    }

    public static void Apply(WeaverWorkspaceState state, WeaverStreamEvent streamEvent)
    {
        if (!string.IsNullOrWhiteSpace(streamEvent.ConversationId))
            state.ConversationId = streamEvent.ConversationId;

        switch (Normalize(streamEvent.Type))
        {
            case "conversation.started":
                state.IsBusy = true;
                state.IsReconnecting = false;
                state.ErrorMessage = null;
                AddActivity(state, streamEvent, "Conversation", "Started");
                break;
            case "assistant.delta":
                AppendAssistantDelta(state, streamEvent);
                break;
            case "assistant.completed":
                AddActivity(state, streamEvent, "Assistant", "Completed");
                break;
            case "tool.started":
            case "tool.progress":
            case "tool.result":
            case "tool.completed":
                UpsertToolActivity(state, streamEvent);
                break;
            case "proposal.created":
            case "proposal.updated":
                UpsertProposal(state, streamEvent);
                break;
            case "conversation.error":
            case "conversation.failed":
                state.ErrorMessage = ReadString(streamEvent.Data, "content") ?? ReadString(streamEvent.Data, "message") ?? "Weaver could not complete the turn.";
                state.IsBusy = false;
                AddActivity(state, streamEvent, "Conversation", "Error", state.ErrorMessage);
                break;
            case "conversation.completed":
                state.IsBusy = false;
                state.PendingLocalMessage = null;
                AddActivity(state, streamEvent, "Conversation", "Completed");
                break;
            default:
                AddActivity(state, streamEvent, streamEvent.Type, "Received", ReadString(streamEvent.Data, "summary") ?? ReadString(streamEvent.Data, "content"));
                break;
        }
    }

    public static bool CanSend(WeaverCapabilities? capabilities, WeaverWorkspaceState state, string message) =>
        capabilities?.Streaming == true && !state.IsBusy && !string.IsNullOrWhiteSpace(message);

    public static bool CanReconnect(WeaverCapabilities? capabilities, WeaverWorkspaceState state) =>
        capabilities?.ConversationPersistence == true && !string.IsNullOrWhiteSpace(state.ConversationId) && !state.IsBusy;

    public static bool SupportsServerQueueing(WeaverCapabilities? capabilities) => false;

    public static bool SupportsSteering(WeaverCapabilities? capabilities) => false;

    private static void AppendAssistantDelta(WeaverWorkspaceState state, WeaverStreamEvent streamEvent)
    {
        var content = ReadString(streamEvent.Data, "content") ?? "";
        var last = state.Messages.LastOrDefault(x => x.Role == WeaverMessageRole.Assistant);
        if (last == null || last.Sequence < streamEvent.Sequence - 1)
        {
            state.Messages.Add(new(Guid.NewGuid().ToString("N"), WeaverMessageRole.Assistant, content, streamEvent.Timestamp, streamEvent.Sequence));
            return;
        }

        var index = state.Messages.Count - 1;
        state.Messages[index] = last with
        {
            Content = last.Content + content,
            Sequence = streamEvent.Sequence,
            Timestamp = streamEvent.Timestamp
        };
    }

    private static void UpsertToolActivity(WeaverWorkspaceState state, WeaverStreamEvent streamEvent)
    {
        var data = streamEvent.Data;
        var id = ReadString(data, "toolCallId") ?? ReadString(data, "id") ?? $"{ReadString(data, "toolName") ?? ReadString(data, "name") ?? "tool"}:{streamEvent.Sequence}";
        var name = ReadString(data, "toolName") ?? ReadString(data, "name") ?? "Tool";
        var status = ReadString(data, "status") ?? streamEvent.Type.Split('.').Last();
        var summary = ReadString(data, "summary") ?? ReadString(data, "content") ?? ReadString(data, "result");
        var item = new WeaverActivityItem
        {
            Id = id,
            Type = "Tool",
            Name = name,
            Status = status,
            Summary = summary,
            Timestamp = streamEvent.Timestamp,
            Data = Clone(data)
        };
        var index = state.Activity.FindIndex(x => x.Id == id);
        if (index >= 0)
            state.Activity[index] = item;
        else
            state.Activity.Add(item);
    }

    private static void UpsertProposal(WeaverWorkspaceState state, WeaverStreamEvent streamEvent)
    {
        var data = streamEvent.Data;
        var id = ReadString(data, "proposalId") ?? ReadString(data, "id") ?? $"proposal:{streamEvent.Sequence}";
        var item = new WeaverProposalItem
        {
            Id = id,
            Status = ReadString(data, "status") ?? "Draft",
            Kind = ReadString(data, "kind") ?? "",
            Summary = ReadString(data, "summary") ?? ReadString(data, "rationale") ?? ReadString(data, "content"),
            WorkflowDefinitionId = ReadString(data, "workflowDefinitionId") ?? ReadString(data, "baselineWorkflowDefinitionId"),
            Timestamp = streamEvent.Timestamp,
            Data = Clone(data)
        };
        var index = state.Proposals.FindIndex(x => x.Id == id);
        if (index >= 0)
            state.Proposals[index] = item;
        else
            state.Proposals.Add(item);
    }

    private static void AddActivity(WeaverWorkspaceState state, WeaverStreamEvent streamEvent, string name, string status, string? summary = null)
    {
        state.Activity.Add(new()
        {
            Id = $"{streamEvent.Type}:{streamEvent.Sequence}",
            Type = streamEvent.Type,
            Name = name,
            Status = status,
            Summary = summary,
            Timestamp = streamEvent.Timestamp,
            Data = Clone(streamEvent.Data)
        });
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string? ReadString(JsonObject data, string name)
    {
        var node = data.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
        return node?.GetValue<string>();
    }

    private static JsonObject Clone(JsonObject data) => (JsonObject)data.DeepClone();
}
