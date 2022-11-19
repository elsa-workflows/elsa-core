namespace Elsa.Workflows.Runtime.Models;

public record StoredBookmark(string ActivityTypeName, string Hash, string WorkflowInstanceId, string BookmarkId, string? CorrelationId = default);