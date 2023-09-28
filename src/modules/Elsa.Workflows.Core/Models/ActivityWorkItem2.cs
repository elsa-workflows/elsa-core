namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents a work item that can be scheduled for execution.
/// </summary>
public class ActivityWorkItem2
{
    /// <summary>
    /// Creates a new instance of the <see cref="ActivityWorkItem"/> class.
    /// </summary>
    /// <param name="activityId">The ID of the activity to execute.</param>
    /// <param name="ownerActivityInstanceId">The ID of the activity instance that owns this work item.</param>
    /// <param name="execute">The delegate to execute.</param>
    /// <param name="tag">An optional tag.</param>
    public ActivityWorkItem2(string activityId, string? ownerActivityInstanceId, Func<ValueTask> execute, object? tag = default)
    {
        ActivityId = activityId;
        OwnerActivityInstanceId = ownerActivityInstanceId;
        Execute = execute;
        Tag = tag;
    }
    
    /// <summary>
    /// Gets the ID of the activity to execute.
    /// </summary>
    public string ActivityId { get; }
    
    /// <summary>
    /// Gets the delegate to execute.
    /// </summary>
    public Func<ValueTask> Execute { get; }
    
    /// <summary>
    /// Gets the ID of the activity instance that owns this work item.
    /// </summary>
    public string? OwnerActivityInstanceId { get; }
    
    /// <summary>
    /// Gets the tag.
    /// </summary>
    public object? Tag { get; }
}