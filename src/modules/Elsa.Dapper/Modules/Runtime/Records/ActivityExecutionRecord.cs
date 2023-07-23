using Elsa.Common.Entities;

namespace Elsa.Dapper.Modules.Runtime.Records;

/// <summary>
/// Represents a single workflow execution, associated with an individual activity instance.
/// </summary>
public class ActivityExecutionRecordRecord
{
    /// <summary>
    /// Gets or sets the ID of the activity execution record.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the workflow instance ID.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the activity ID.
    /// </summary>
    public string ActivityId { get; set; } = default!;

    /// <summary>
    /// The type of the activity.
    /// </summary>
    public string ActivityType { get; set; } = default!;

    /// <summary>
    /// The version of the activity type.
    /// </summary>
    public int ActivityTypeVersion { get; set; }

    /// <summary>
    /// The name of the activity.
    /// </summary>
    public string? ActivityName { get; set; } = default!;
    
    /// <summary>
    /// The state of the activity at the time this record is created or last updated.
    /// </summary>
    public string? ActivityState { get; set; }

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