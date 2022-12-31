using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class TriggerExtensions
{
    public static IEnumerable<Trigger> Filter<T>(this IEnumerable<Trigger> triggers) where T : IActivity
    {
        var activityTypeName = TypeNameHelper.GenerateTypeName<T>();
        return triggers.Where(x => x.Type == activityTypeName);
    }
}