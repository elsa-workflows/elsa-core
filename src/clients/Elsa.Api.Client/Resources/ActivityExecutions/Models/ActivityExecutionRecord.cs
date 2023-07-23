using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.ActivityExecutions.Models;

/// <summary>
/// Represents a single workflow execution, associated with an individual activity instance.
/// </summary>
public class ActivityExecutionRecord : Entity
{
    /// <summary>
    /// Gets or sets the workflow instance ID.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the activity ID.
    /// </summary>
    public string ActivityId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the activity type version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the time at which the activity execution began.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether the activity has any bookmarks.
    /// </summary>
    public bool HasBookmarks { get; set; }
    
    /// <summary>
    /// Gets or sets the time at which the activity execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}