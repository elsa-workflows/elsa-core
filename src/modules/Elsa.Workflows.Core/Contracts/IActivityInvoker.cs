namespace Elsa.Workflows.Core.Contracts;

public interface IActivityInvoker
{
    Task InvokeAsync(
        WorkflowExecutionContext workflowExecutionContext,
        IActivity activity,
        ActivityExecutionContext? owner = default);

    Task InvokeAsync(ActivityExecutionContext activityExecutionContext);
}