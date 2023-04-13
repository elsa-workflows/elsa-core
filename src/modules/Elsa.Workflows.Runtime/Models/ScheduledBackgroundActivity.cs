namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Represents a scheduled background activity
/// </summary>
/// <param name="WorkflowInstanceId">The ID of the workflow instance containing the activity to execute.</param>
/// <param name="ActivityNodeId">The ID of the activity to execute.</param>
/// <param name="BookmarkId">The ID of the bookmark to resume.</param>
public record ScheduledBackgroundActivity(string WorkflowInstanceId, string ActivityNodeId, string BookmarkId);