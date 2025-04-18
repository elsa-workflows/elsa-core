using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Runtime.Records;

/// <summary>
/// Represents a single workflow execution, associated with an individual activity instance.
/// </summary>
internal class ActivityExecutionSummaryRecord :  Record
{
    /// <summary>
    /// Gets or sets the workflow instance ID.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the activity ID.
    /// </summary>
    public string ActivityId { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the activity node ID.
    /// </summary>
    public string ActivityNodeId { get; set; } = null!;

    /// <summary>
    /// The type of the activity.
    /// </summary>
    public string ActivityType { get; set; } = null!;

    /// <summary>
    /// The version of the activity type.
    /// </summary>
    public int ActivityTypeVersion { get; set; }

    /// <summary>
    /// The name of the activity.
    /// </summary>
    public string? ActivityName { get; set; }

    /// <summary>
    /// Gets or sets the time at which the activity execution began.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the activity has any bookmarks.
    /// </summary>
    public bool HasBookmarks { get; set; }

    /// <summary>
    /// Gets or sets the status of the activity.
    /// </summary>
    public string Status { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the time at which the activity execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}