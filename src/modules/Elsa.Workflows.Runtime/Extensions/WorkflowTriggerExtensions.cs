using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class WorkflowTriggerExtensions
{
    public static IEnumerable<StoredTrigger> Filter<T>(this IEnumerable<StoredTrigger> triggers) where T : ITrigger
    {
        var triggerName = ActivityTypeNameHelper.GenerateTypeName<T>();
        return triggers.Where(x => x.Name == triggerName);
    }
}