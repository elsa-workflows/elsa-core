using System.Reflection;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Activities.Flowchart.Services;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Implementations;

public class ActivityNodeDescriber : IActivityNodeDescriber
{
    public ActivityNodeDescriptor DescribeActivity(Type activityType)
    {
        var fullTypeName = ActivityTypeNameHelper.GenerateTypeName(activityType);

        var outboundPorts =
            from prop in activityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where typeof(IActivity).IsAssignableFrom(prop.PropertyType) || typeof(IEnumerable<IActivity>).IsAssignableFrom(prop.PropertyType)
            let portAttr = prop.GetCustomAttribute<OutboundAttribute>()
            where portAttr != null
            select new Port
            {
                Name = portAttr.Name ?? prop.Name,
                DisplayName = portAttr.DisplayName ?? portAttr.Name ?? prop.Name
            };

        return new ActivityNodeDescriptor(activityType, fullTypeName, outboundPorts.ToList());
    }
}