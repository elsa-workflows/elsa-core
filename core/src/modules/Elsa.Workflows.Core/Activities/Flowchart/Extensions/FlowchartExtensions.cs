namespace Elsa.Workflows.Activities.Flowchart.Extensions;

public static class FlowchartExtensions
{
    public static IActivity? GetRootActivity(this Activities.Flowchart flowchart)
    {
        // Get the first activity that has no inbound connections.
        var query =
            from activity in flowchart.Activities
            let inboundConnections = flowchart.Connections.Any(x => x.Target.Activity == activity)
            where !inboundConnections
            select activity;

        var rootActivity = query.FirstOrDefault();
        return rootActivity;
    }
}