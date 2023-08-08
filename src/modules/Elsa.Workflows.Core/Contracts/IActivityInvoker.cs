namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Invokes activities.
/// </summary>
public interface IActivityInvoker
{
    /// <summary>
    /// Invokes the specified activity.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context.</param>
    /// <param name="activity">The activity to invoke.</param>
    /// <param name="owner">The activity execution context that owns the activity.</param>
    /// <param name="tag">An optional tag to associate with the activity execution.</param>
    Task InvokeAsync(WorkflowExecutionContext workflowExecutionContext,
        IActivity activity,
        ActivityExecutionContext? owner = default, 
        object? tag = default);

    /// <summary>
    /// Invokes the specified activity execution context.
    /// </summary>
    Task InvokeAsync(ActivityExecutionContext activityExecutionContext);
}