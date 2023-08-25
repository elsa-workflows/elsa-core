namespace Elsa.Workflows.Core;

/// <summary>
/// Provides context information when an activity has completed.
/// </summary>
public record ActivityCompletedContext(ActivityExecutionContext TargetContext, ActivityExecutionContext ChildContext, object? Result = default)
{
    /// <summary>
    /// Gets the workflow execution context.
    /// </summary>
    public WorkflowExecutionContext WorkflowExecutionContext => TargetContext.WorkflowExecutionContext;
}