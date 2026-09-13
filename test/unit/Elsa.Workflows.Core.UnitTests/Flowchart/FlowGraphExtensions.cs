using Elsa.Workflows.Activities.Flowchart.Models;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Flowchart;

public static class FlowGraphExtensions
{
    public static async Task ValidateOutboundConnections(this FlowGraph flowGraph, List<string> expected, IActivity activity)
    {
        await Assert.That(flowGraph.GetOutboundConnections(activity).Select(c => c.ToString()))
            .IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    public static async Task ValidateForwardInboundConnections(this FlowGraph flowGraph, List<string> expected, IActivity activity)
    {
        await Assert.That(flowGraph.GetForwardInboundConnections(activity).Select(c => c.ToString()))
            .IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
    public static async Task ValidateBackwardConnection(this FlowGraph flowGraph, bool expectedBackward, bool expectedValid, Connection connection)
    {
        var actualBackward = flowGraph.IsBackwardConnection(connection, out var actualValid);
        await Assert.That(actualBackward).IsEqualTo(expectedBackward);
        await Assert.That(actualValid).IsEqualTo(expectedValid);
    }

    public static async Task ValidateDanglingActivity(this FlowGraph flowGraph, bool expected, Activity activity)
    {
        await Assert.That(flowGraph.IsDanglingActivity(activity)).IsEqualTo(expected);
    }

    public static async Task ValidateAncestorActivities(this FlowGraph flowGraph, List<Activity> expected, Activity activity)
    {
        await Assert.That(flowGraph.GetAncestorActivities(activity))
            .IsEquivalentTo(expected.Cast<IActivity>(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
}
