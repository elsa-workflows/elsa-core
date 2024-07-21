using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Runtime.Records;

/// <summary>
/// Represents a single workflow execution, associated with an individual activity instance.
/// </summary>
internal class ActivityExecutionRecordRecord : Record
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
    /// Gets or sets the activity node ID.
    /// </summary>
    public string ActivityNodeId { get; set; } = default!;

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
    public string? ActivityName { get; set; }
    
    /// <summary>
    /// The state of the activity at the time this record is created or last updated.
    /// </summary>
    public string? SerializedActivityState { get; set; }
    
    /// <summary>
    /// Any additional payload associated with the log record.
    /// </summary>
    public string? SerializedPayload { get; set; }
    
    /// <summary>
    /// Any outputs provided by the activity.
    /// </summary>
    public string? SerializedOutputs { get; set; }

    /// <summary>
    /// Gets or sets the exception that occurred during the activity execution.
    /// </summary>
    public string? SerializedException { get; set; }

    /// <summary>
    /// Any properties provided by the activity.
    /// </summary>
    public string? SerializedProperties { get; set; }

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
    public string Status { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the time at which the activity execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}