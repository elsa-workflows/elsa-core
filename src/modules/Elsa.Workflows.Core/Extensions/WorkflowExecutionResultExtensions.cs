using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public static class WorkflowExecutionResultExtensions
{
    public static T GetActivityOutput<T>(this RunWorkflowResult result, IActivity activity, string? outputName = null)
    {
        return (T)result.WorkflowExecutionContext.GetOutputByActivityId(activity.Id, outputName)!;
    }
}