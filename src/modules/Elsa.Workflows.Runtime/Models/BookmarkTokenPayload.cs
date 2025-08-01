namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents the payload for a bookmark token, including the bookmark identifier and the associated workflow instance identifier.
/// </summary>
public record BookmarkTokenPayload(string BookmarkId, string WorkflowInstanceId);