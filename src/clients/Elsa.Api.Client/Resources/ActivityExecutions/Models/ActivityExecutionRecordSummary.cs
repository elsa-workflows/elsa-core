using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.ActivityExecutions.Models;

/// <summary>
/// Represents a summarized view of a single activity execution of an activity instance.
/// </summary>
public class ActivityExecutionRecordSummary : Entity
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
    /// Gets or sets a collection of properties for the activity execution.
    /// </summary>
    public IDictionary<string, object?>? Properties { get; set; }

    /// <summary>
    /// Gets or sets the status of the activity.
    /// </summary>
    public ActivityStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the time at which the activity execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Returns a summary view of the specified <see cref="ActivityExecutionRecord"/>.
    /// </summary>
    public static ActivityExecutionRecordSummary FromRecord(ActivityExecutionRecord record)
    {
        return new()
        {
            Id = record.Id,
            WorkflowInstanceId = record.WorkflowInstanceId,
            ActivityId = record.ActivityId,
            ActivityNodeId = record.ActivityNodeId,
            ActivityType = record.ActivityType,
            ActivityTypeVersion = record.ActivityTypeVersion,
            ActivityName = record.ActivityName,
            StartedAt = record.StartedAt,
            HasBookmarks = record.HasBookmarks,
            Status = record.Status,
            CompletedAt = record.CompletedAt,
            Properties = record.Properties
        };
    }
}