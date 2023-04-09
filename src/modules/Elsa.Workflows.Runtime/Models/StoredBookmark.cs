namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Represents a bookmark that has been stored in the database.
/// </summary>
public record StoredBookmark(string ActivityTypeName, string Hash, string WorkflowInstanceId, string BookmarkId, string? CorrelationId = default, string? Data = default);