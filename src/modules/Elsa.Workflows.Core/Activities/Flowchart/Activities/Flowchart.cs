using Elsa.Workflows.Core.Activities.Flowchart.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

[Activity("Elsa", "Flow", "A flowchart is a collection of activities and connections between them.")]
public class Flowchart : Container
{
    private const string ScopesProperty = "Scopes";
    private const string ScopeProperty = "Scope";

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
        var flowchartActivityExecutionContext = context.ReceiverActivityExecutionContext;
        var completedActivity = context.SenderActivityExecutionContext.Activity;

        if (completedActivity == null!)
            return;

        // Ignore completed activities that are not immediate children.
        var isDirectChild = Activities.Contains(completedActivity);

        if (!isDirectChild)
            return;

        // If a specific outcome was provided by the completed activity, use it to find the connection to the next activity.
        Func<Connection, bool> outboundConnectionsQuery = signal.Result is Outcome outcome
            ? connection => connection.Source == completedActivity && connection.SourcePort == outcome.Name
            : connection => connection.Source == completedActivity;

        var outboundConnections = Connections.Where(outboundConnectionsQuery).ToList();
        var children = outboundConnections.Select(x => x.Target).ToList();
        var scope = flowchartActivityExecutionContext.GetProperty(ScopeProperty, () => new FlowScope());
        
        scope.RegisterActivityExecution(completedActivity);
        
        if (children.Any())
        {
            
            scope.AddActivities(children);

            // Only schedule children that have not yet already executed in the current scope.
            var filteredChildren = scope.ExcludeExecutedActivities(children).ToList();
            children = filteredChildren;

            var executedActivities = scope.Activities.Values.Where(x => x.HasExecuted).Select(x => x.ActivityId).ToList();
            
            // Schedule each child, but only if all of its inbound activities have already executed.
            foreach (var activity in children)
            {
                var inboundActivities = Connections.InboundActivities(activity).ToList();
                var haveInboundActivitiesExecuted = inboundActivities.All(x => executedActivities.Contains(x.Id));
                
                if(haveInboundActivitiesExecuted)
                    flowchartActivityExecutionContext.ScheduleActivity(activity);
            }
        }

        if (!children.Any())
        {
            // If there are no more pending activities in any of the scopes, mark this activity as completed.
            var hasPendingChildren = scope.HasPendingActivities();

            if (!hasPendingChildren)
                await flowchartActivityExecutionContext.CompleteActivityAsync();
        }

        context.StopPropagation();
    }
}