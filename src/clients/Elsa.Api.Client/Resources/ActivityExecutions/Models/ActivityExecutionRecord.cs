using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.ActivityExecutions.Models;

/// <summary>
/// Represents a single workflow execution, associated with an individual activity instance.
/// </summary>
[UsedImplicitly]
public class ActivityExecutionRecord : Entity
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
    public string? ActivityName { get; set; } = null!;
    
    /// <summary>
    /// The state of the activity at the time this record is created or last updated.
    /// </summary>
    public IDictionary<string, object?>? ActivityState { get; set; }
    
    /// <summary>
    /// Any additional payload associated with the log record.
    /// </summary>
    public IDictionary<string, object?>? Payload { get; set; }
    
    /// <summary>
    /// Any outputs provided by the activity.
    /// </summary>
    public IDictionary<string, object?>? Outputs { get; set; }

    /// <summary>
    /// Any properties provided by the activity.
    /// </summary>
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Any metadata provided by the activity. In contrast to the <see cref="Properties"/> property, this metadata is also persisted as part of the activity execution summary record.
    /// It is therefore not suitable for larger payloads.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Gets or sets the exception that occurred during the activity execution.
    /// </summary>
    public ExceptionState? Exception { get; set; }

    /// <summary>
    /// Gets or sets the time at which the activity execution began.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether the activity has any bookmarks.
    /// </summary>
    public bool HasBookmarks { get; set; }
    
    /// <summary>
    /// Gets or sets the status of the activity.
    /// </summary>
    public ActivityStatus Status { get; set; }

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
    /// This represents the temporal/execution predecessor that directly triggered execution of this activity.
    /// </summary>
    public string? SchedulingActivityExecutionId { get; set; }

    /// <summary>
    /// The ID of the activity that scheduled this activity execution (denormalized for convenience).
    /// </summary>
    public string? SchedulingActivityId { get; set; }

    /// <summary>
    /// The workflow instance ID of the workflow that scheduled this activity execution.
    /// This is set when crossing workflow boundaries (e.g., via ExecuteWorkflow or DispatchWorkflow).
    /// </summary>
    public string? SchedulingWorkflowInstanceId { get; set; }

    /// <summary>
    /// The depth of this activity in the call stack (0 for root activities).
    /// Calculated by traversing the SchedulingActivityExecutionId chain until reaching null.
    /// </summary>
    public int? CallStackDepth { get; set; }
}
