using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Core.UnitTests.Flowchart;

public static class FlowGraphExtensions
{
    public static void ValidateOutboundConnections(this FlowGraph flowGraph, List<string> expected, IActivity activity)
    {
        Assert.Equal(expected, flowGraph.GetOutboundConnections(activity).Select(c => c.ToString()));
    }

    public static void ValidateForwardInboundConnections(this FlowGraph flowGraph, List<string> expected, IActivity activity)
    {
        Assert.Equal(expected, flowGraph.GetForwardInboundConnections(activity).Select(c => c.ToString()));
    }
    public static void ValidateBackwardConnection(this FlowGraph flowGraph, bool expectedBackward, bool expectedValid, Connection connection)
    {
        var actualBackward = flowGraph.IsBackwardConnection(connection, out var actualValid);
        Assert.Equal(expectedBackward, actualBackward);
        Assert.Equal(expectedValid, actualValid);
    }

    public static void ValidateDanglingActivity(this FlowGraph flowGraph, bool expected, Activity activity)
    {
        Assert.Equal(expected, flowGraph.IsDanglingActivity(activity));
    }

    public static void ValidateAncestorActivities(this FlowGraph flowGraph, List<Activity> expected, Activity activity)
    {
        Assert.Equal(expected, flowGraph.GetAncestorActivities(activity));
    }
}
