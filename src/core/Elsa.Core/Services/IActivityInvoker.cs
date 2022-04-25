using Elsa.Models;

namespace Elsa.Services;

public interface IActivityInvoker
{
    Task InvokeAsync(
        WorkflowExecutionContext workflowExecutionContext,
        IActivity activity,
        ActivityExecutionContext? owner = default,
        IEnumerable<RegisterLocationReference>? locationReferences = default);

    Task InvokeAsync(ActivityExecutionContext activityExecutionContext);
}