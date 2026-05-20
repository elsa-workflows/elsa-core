namespace Elsa.Workflows.Runtime;

/// <summary>
/// The result of replaying a bookmark queue dead-letter item.
/// </summary>
public record ReplayBookmarkQueueDeadLetterResult(bool Succeeded, string? QueueItemId, string? Reason);
