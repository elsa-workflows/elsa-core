using System.Text.Json.Nodes;

namespace Elsa.Studio.AI.Models;

public class WeaverWorkspaceState
{
    public string? ConversationId { get; set; }
    public bool IsBusy { get; set; }
    public bool IsReconnecting { get; set; }
    public string? ErrorMessage { get; set; }
    public List<WeaverChatMessage> Messages { get; } = [];
    public List<WeaverActivityItem> Activity { get; } = [];
    public List<WeaverProposalItem> Proposals { get; } = [];
    public List<WeaverContextAttachment> Attachments { get; } = [];
    public string? PendingLocalMessage { get; set; }
}

public record WeaverChatMessage(string Id, WeaverMessageRole Role, string Content, DateTimeOffset Timestamp, long Sequence);

public record WeaverActivityItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Type { get; init; } = "";
    public string Name { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Summary { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public JsonObject Data { get; init; } = [];
}

public record WeaverProposalItem
{
    public string Id { get; init; } = "";
    public string Status { get; init; } = "Draft";
    public string Kind { get; init; } = "";
    public string? Summary { get; init; }
    public string? WorkflowDefinitionId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public JsonObject Data { get; init; } = [];
}

public enum WeaverMessageRole
{
    User,
    Assistant,
    System
}
