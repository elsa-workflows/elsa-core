using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.ActivityNodeResolvers;

public class OutboundActivityPortResolver : IActivityPortResolver
{
    public int Priority => -1;
    public bool GetSupportsActivity(IActivity activity) => true;

    public ValueTask<IEnumerable<IActivity>> GetPortsAsync(IActivity activity, CancellationToken cancellationToken = default) =>
        new(GetSinglePorts(activity)
            .Where(x => x != null)
            .Select(x => x!)
            .ToHashSet());


    private static IEnumerable<IActivity?> GetSinglePorts(IActivity activity)
    {
        var activityType = activity.GetType();

        var ports =
            from prop in activityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where typeof(IActivity).IsAssignableFrom(prop.PropertyType) || typeof(IEnumerable<IActivity>).IsAssignableFrom(prop.PropertyType)
            let portAttr = prop.GetCustomAttribute<PortAttribute>()
            let nodeAttr = prop.GetCustomAttribute<NodeAttribute>()
            where portAttr != null || nodeAttr != null
            let value = prop.GetValue(activity)
            let isCollection = GetPropertyIsCollection(prop.PropertyType)
            select isCollection ? (IEnumerable<IActivity>)value : new[] { (IActivity)value };

        return ports.SelectMany(x => x);
    }

    private static bool GetPropertyIsCollection(Type propertyType) => typeof(IEnumerable<IActivity>).IsAssignableFrom(propertyType);
}