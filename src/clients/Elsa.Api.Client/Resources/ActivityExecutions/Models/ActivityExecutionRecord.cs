using Elsa.Api.Client.Resources.WorkflowInstances.Models;
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
    /// Gets or sets the time at which the activity execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}