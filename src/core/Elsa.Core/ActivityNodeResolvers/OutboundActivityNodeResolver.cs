using System.Reflection;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.ActivityNodeResolvers;

public class OutboundActivityNodeResolver : IActivityNodeResolver
{
    public int Priority => -1;
    public bool GetSupportsActivity(IActivity activity) => activity is Activity;

    public IEnumerable<IActivity> GetPorts(IActivity activity) =>
        GetSinglePorts(activity)
            .Where(x => x != null)
            .Select(x => x!)
            .ToHashSet();

    private static IEnumerable<IActivity?> GetSinglePorts(IActivity activity)
    {
        var nodeType = activity.GetType();

        var ports =
            from prop in nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where typeof(IActivity).IsAssignableFrom(prop.PropertyType) || typeof(IEnumerable<IActivity>).IsAssignableFrom(prop.PropertyType)
            let portAttr = prop.GetCustomAttribute<OutboundAttribute>()
            let nodeAttr = prop.GetCustomAttribute<NodeAttribute>()
            where portAttr != null || nodeAttr != null
            let value = prop.GetValue(activity)
            let isCollection = GetPropertyIsCollection(prop.PropertyType)
            select isCollection ? (IEnumerable<IActivity>)value : new[] { (IActivity)value };

        return ports.SelectMany(x => x);
    }

    private static bool GetPropertyIsCollection(Type propertyType) => typeof(IEnumerable<IActivity>).IsAssignableFrom(propertyType);
}