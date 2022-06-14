using Elsa.Workflows.Core.Activities.Flowchart.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Activities.Flowchart.Services;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Implementations;

using Flowchart = Activities.Flowchart;

public class Transformer : ITransformer
{
    private readonly IActivityNodeDescriber _activityNodeDescriber;
    private readonly IIdentityGenerator _identityGenerator;

    public Transformer(IActivityNodeDescriber activityNodeDescriber, IIdentityGenerator identityGenerator)
    {
        _activityNodeDescriber = activityNodeDescriber;
        _identityGenerator = identityGenerator;
    }

    public void Transpose(Flowchart flowchart)
    {
        var start = flowchart.Start;

        if (start == null)
            return;

        var activities = flowchart.Activities;
        var activityTypes = activities.Select(x => x.GetType()).Distinct().ToList();
        var activityDescriptors = activityTypes.Select(_activityNodeDescriber.DescribeActivity).ToDictionary(x => x.ActivityRuntimeType);

        TransposeInternal(flowchart, activityDescriptors);
    }

    private void TransposeInternal(Flowchart flowchart, IDictionary<Type, ActivityNodeDescriptor> descriptors)
    {
        var connections = flowchart.Connections.ToList();

        foreach (var connection in connections)
        {
            var portName = connection.SourcePort;

            // If the connection has no source port name, it cannot be mapped to an outbound port property on an activity. 
            if (portName == null)
                continue;

            var source = connection.Source;
            var sourceType = source.GetType();
            var sourceDescriptor = descriptors[sourceType];
            var sourcePort = sourceDescriptor.OutPorts.FirstOrDefault(x => x.Name == portName);

            // If the connection source port does not match any outbound port properties on the source activity, the target cannot be transposed onto it. 
            if (sourcePort == null)
                continue;

            var target = connection.Target;
            var descendantConnections = connections.Descendants(target).ToList();

            // Remove all descendant connections and activities from the current flowchart level.
            // They are either no longer necessary because they are transposed, or they will be added to a child flowchart.
            flowchart.Connections.Remove(connection);
            flowchart.Activities.Remove(target);

            if (!descendantConnections.Any())
            {
                // There were no descendant connections, which means we can transpose the current target as-is.
                source.SetPropertyValue(portName, target);
            }
            else
            {
                // There was at least one descendant, which means we need to capture them into a child flow.
                // TODO: Add logic to try and determine if we can add a sequence instead.
                var activities = descendantConnections.Select(x => x.Source).Concat(descendantConnections.Select(x => x.Target)).ToHashSet();

                var subFlow = new Flowchart
                {
                    Id = _identityGenerator.GenerateId(),
                    Activities = activities,
                    Connections = descendantConnections,
                    Start = target
                };

                // Remove transported connections and activities from the current flowchart.
                flowchart.Connections = flowchart.Connections.Except(descendantConnections).ToList();
                flowchart.Activities = flowchart.Activities.Except(activities).ToHashSet();

                // Transpose the child flow into the current source.
                source.SetPropertyValue(portName, subFlow);

                // Recurse into the subflow.
                TransposeInternal(subFlow, descriptors);
            }
        }
    }
}