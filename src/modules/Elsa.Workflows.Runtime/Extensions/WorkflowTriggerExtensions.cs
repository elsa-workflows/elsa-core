using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Extensions;

public static class WorkflowTriggerExtensions
{
    public static IEnumerable<WorkflowTrigger> Filter<T>(this IEnumerable<WorkflowTrigger> triggers) where T : ITrigger
    {
        var triggerName = ActivityTypeNameHelper.GenerateTypeName<T>();
        return triggers.Where(x => x.Name == triggerName);
    }
}