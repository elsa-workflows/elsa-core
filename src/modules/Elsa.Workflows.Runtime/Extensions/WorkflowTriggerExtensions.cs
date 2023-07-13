using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="StoredTrigger"/>.
/// </summary>
public static class WorkflowTriggerExtensions
{
    /// <summary>
    /// Filters a collection of <see cref="StoredTrigger"/> by trigger type.
    /// </summary>
    /// <param name="triggers">The collection of triggers to filter.</param>
    /// <typeparam name="T">The trigger type.</typeparam>
    public static IEnumerable<StoredTrigger> Filter<T>(this IEnumerable<StoredTrigger> triggers) where T : ITrigger
    {
        var triggerName = ActivityTypeNameHelper.GenerateTypeName<T>();
        return triggers.Where(x => x.Name == triggerName);
    }
}