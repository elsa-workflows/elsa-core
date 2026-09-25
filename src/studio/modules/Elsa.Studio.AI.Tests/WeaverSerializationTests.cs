using Elsa.Studio.AI.Models;
using Xunit;

namespace Elsa.Studio.AI.Tests;

public class WeaverSerializationTests
{
    [Fact]
    public void Capabilities_DeserializesCoreResponseShape()
    {
        const string json = """
        {
          "streaming": true,
          "conversationPersistence": true,
          "proposalReview": true,
          "supportedAttachmentKinds": ["WorkflowDefinition", "WorkflowInstance"],
          "agents": [
            {
              "name": "workflow-author",
              "displayName": "Workflow author",
              "description": "Creates safe workflow proposals"
            }
          ]
        }
        """;

        var capabilities = System.Text.Json.JsonSerializer.Deserialize(json, WeaverJsonContext.Default.WeaverCapabilities)!;

        Assert.True(capabilities.Streaming);
        Assert.True(capabilities.ConversationPersistence);
        Assert.True(capabilities.ProposalReview);
        Assert.True(capabilities.SupportsAttachmentKind("WorkflowInstance"));
        Assert.Equal("workflow-author", Assert.Single(capabilities.Agents).Name);
    }

    [Fact]
    public void StreamEvent_DeserializesSsePayload()
    {
        const string json = """
        {
          "type": "assistant.delta",
          "conversationId": "conversation-123",
          "sequence": 12,
          "timestamp": "2026-06-08T10:15:00Z",
          "data": {
            "messageId": "message-456",
            "content": "The failure happened in..."
          }
        }
        """;

        var streamEvent = System.Text.Json.JsonSerializer.Deserialize(json, WeaverJsonContext.Default.WeaverStreamEvent)!;

        Assert.Equal("assistant.delta", streamEvent.Type);
        Assert.Equal("conversation-123", streamEvent.ConversationId);
        Assert.Equal(12, streamEvent.Sequence);
        Assert.Equal("The failure happened in...", streamEvent.Data["content"]!.GetValue<string>());
    }
}
