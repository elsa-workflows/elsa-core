using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

[Activity("Elsa", "Workflows", "A flowchart is a collection of activities and connections between them.")]
public class Flowchart : Container
{
    public Flowchart()
    {
        OnSignalReceived<ActivityCompleted>(OnDescendantCompletedAsync);
    }

    [Node] public IActivity? Start { get; set; }
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();

    protected override async ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        if (Start == null!)
        {
            await context.CompleteActivityAsync();
            return;
        }

        context.ScheduleActivity(Start);
    }

    private async ValueTask OnDescendantCompletedAsync(ActivityCompleted signal, SignalContext context)
    {
        await ScheduleChildrenAsync(signal, context);
    }
    
    private async Task ScheduleChildrenAsync(ActivityCompleted signal, SignalContext context)
    {
        var activityExecutionContext = context.ReceiverActivityExecutionContext;
        var parent = context.SenderActivityExecutionContext.Activity;

        if (parent == null!)
            return;

        // Ignore completed activities that are not immediate children.
        var isDirectChild = Activities.Contains(parent);
        
        if (!isDirectChild)
            return;

        // If a specific outcome was provided by the completed activity, use it to find the connection to the next activity.
        Func<Connection, bool> outboundConnectionsQuery = signal.Result is Outcome outcome 
            ? connection => connection.Source == parent && connection.SourcePort == outcome.Name 
            : connection => connection.Source == parent;

        var outboundConnections = Connections.Where(outboundConnectionsQuery).ToList();
        var children = outboundConnections.Select(x => x.Target).ToList();

        if (children.Any())
            activityExecutionContext.ScheduleActivities(children);
        else
            await activityExecutionContext.CompleteActivityAsync();

        context.StopPropagation();
    }
}