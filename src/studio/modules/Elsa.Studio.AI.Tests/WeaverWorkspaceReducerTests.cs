using Elsa.Studio.AI.Models;
using Elsa.Studio.AI.Services;
using System.Text.Json.Nodes;
using Xunit;

namespace Elsa.Studio.AI.Tests;

public class WeaverWorkspaceReducerTests
{
    private readonly WeaverWorkspaceState _state = new();
    private readonly DateTimeOffset _timestamp = DateTimeOffset.Parse("2026-06-08T10:00:00Z");

    [Fact]
    public void Apply_AssistantDeltas_AppendsToActiveAssistantMessage()
    {
        Apply("assistant.delta", 1, new JsonObject { ["content"] = "First " });
        Apply("assistant.delta", 2, new JsonObject { ["content"] = "second" });

        var message = Assert.Single(_state.Messages);
        Assert.Equal(WeaverMessageRole.Assistant, message.Role);
        Assert.Equal("First second", message.Content);
        Assert.Equal(2, message.Sequence);
    }

    [Fact]
    public void Apply_ToolEvents_UpsertsToolActivity()
    {
        Apply("tool.started", 1, new JsonObject
        {
            ["toolCallId"] = "tool-1",
            ["toolName"] = "workflow.getDefinition",
            ["status"] = "Running"
        });
        Apply("tool.result", 2, new JsonObject
        {
            ["toolCallId"] = "tool-1",
            ["toolName"] = "workflow.getDefinition",
            ["status"] = "Completed",
            ["summary"] = "Loaded workflow"
        });

        var activity = Assert.Single(_state.Activity);
        Assert.Equal("tool-1", activity.Id);
        Assert.Equal("workflow.getDefinition", activity.Name);
        Assert.Equal("Completed", activity.Status);
        Assert.Equal("Loaded workflow", activity.Summary);
    }

    [Fact]
    public void Apply_ProposalEvents_UpsertsProposalState()
    {
        Apply("proposal.created", 1, new JsonObject
        {
            ["proposalId"] = "proposal-1",
            ["kind"] = "WorkflowUpdate",
            ["status"] = "Draft",
            ["summary"] = "Add approval branch"
        });
        Apply("proposal.updated", 2, new JsonObject
        {
            ["proposalId"] = "proposal-1",
            ["kind"] = "WorkflowUpdate",
            ["status"] = "Approved"
        });

        var proposal = Assert.Single(_state.Proposals);
        Assert.Equal("proposal-1", proposal.Id);
        Assert.Equal("WorkflowUpdate", proposal.Kind);
        Assert.Equal("Approved", proposal.Status);
    }

    [Fact]
    public void Apply_ConversationCompleted_ClearsBusyAndPendingMessage()
    {
        _state.IsBusy = true;
        _state.PendingLocalMessage = "queued locally";

        Apply("conversation.completed", 4, []);

        Assert.False(_state.IsBusy);
        Assert.Null(_state.PendingLocalMessage);
    }

    [Fact]
    public void CanSend_WhenStreamingDisabled_ReturnsFalse()
    {
        var capabilities = new WeaverCapabilities(false, true, true, ["WorkflowDefinition"], []);

        var canSend = WeaverWorkspaceReducer.CanSend(capabilities, _state, "Explain this");

        Assert.False(canSend);
    }

    [Fact]
    public void CanReconnect_WhenConversationPersistenceEnabledAndConversationExists_ReturnsTrue()
    {
        _state.ConversationId = "conversation-1";
        var capabilities = new WeaverCapabilities(true, true, false, ["WorkflowDefinition"], []);

        var canReconnect = WeaverWorkspaceReducer.CanReconnect(capabilities, _state);

        Assert.True(canReconnect);
    }

    [Fact]
    public void SteeringAndQueueing_AreFalseUntilBackendExposesContracts()
    {
        var capabilities = new WeaverCapabilities(true, true, true, ["WorkflowDefinition"], []);

        Assert.False(WeaverWorkspaceReducer.SupportsSteering(capabilities));
        Assert.False(WeaverWorkspaceReducer.SupportsServerQueueing(capabilities));
    }

    private void Apply(string type, long sequence, JsonObject data)
    {
        WeaverWorkspaceReducer.Apply(_state, new()
        {
            Type = type,
            ConversationId = "conversation-1",
            Sequence = sequence,
            Timestamp = _timestamp.AddSeconds(sequence),
            Data = data
        });
    }
}
