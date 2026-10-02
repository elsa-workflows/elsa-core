using Elsa.Persistence.Dapper.Records;

namespace Elsa.Persistence.Dapper.Modules.Runtime.Records;

/// <summary>
/// Represents a single workflow execution, associated with an individual activity instance.
/// </summary>
internal class ActivityExecutionRecordRecord : Record
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
    /// Lightweight metadata associated with the activity execution.
    /// </summary>
    public string? SerializedMetadata { get; set; }

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
    /// Gets or sets the aggregated count of faults encountered during the execution of the activity instance and its descendants.
    /// </summary>
    public int AggregateFaultCount { get; set; }

    /// <summary>
    /// Gets or sets the time at which the activity execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The ID of the activity execution context that scheduled this activity execution.
    /// </summary>
    public string? SchedulingActivityExecutionId { get; set; }

    /// <summary>
    /// The ID of the activity that scheduled this activity execution (denormalized).
    /// </summary>
    public string? SchedulingActivityId { get; set; }

    /// <summary>
    /// The workflow instance ID of the workflow that scheduled this activity execution.
    /// </summary>
    public string? SchedulingWorkflowInstanceId { get; set; }

    /// <summary>
    /// The depth of this activity in the call stack (0 for root activities).
    /// </summary>
    public int? CallStackDepth { get; set; }
}
