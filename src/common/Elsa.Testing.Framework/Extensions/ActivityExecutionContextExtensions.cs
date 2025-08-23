using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace Elsa.Testing.Framework.Extensions;

public static class ActivityExecutionContextExtensions
{
    public static ActivityExecutionContext? FindActivityExecutionContext(this WorkflowExecutionContext workflowExecutionContext, string activityId)
    {
        var activityHandle = ActivityHandle.FromActivityId(activityId);
        return workflowExecutionContext.FindActivityExecutionContext(activityHandle);
    }
    
    public static ActivityExecutionContext? FindActivityExecutionContext(this WorkflowExecutionContext workflowExecutionContext, ActivityHandle activityHandle)
    {
       return workflowExecutionContext.FindActivityExecutionContexts(activityHandle).LastOrDefault();
    }
}