using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Contracts;
using Elsa.Workflows.Core.Activities.Flowchart.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Signals;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

/// <summary>
/// A flowchart consists of a collection of activities and connections between them.
/// </summary>
[Activity("Elsa", "Flow", "A flowchart is a collection of activities and connections between them.")]
[PublicAPI]
[Browsable(false)]
public class Flowchart : Container
{
    internal const string ScopeProperty = "Scope";

    /// <inheritdoc />
    [JsonConstructor]
    public Flowchart() : this(default, default)
    {
    }

    /// <inheritdoc />
    public Flowchart([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        OnSignalReceived<ActivityCompleted>(OnDescendantCompletedAsync);
    }

    /// <summary>
    /// The activity to execute when the workflow starts.
    /// </summary>
    [Port]
    [Browsable(false)]
    public IActivity? Start { get; set; }

    /// <summary>
    /// A list of connections between activities.
    /// </summary>
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();

    /// <inheritdoc />
    protected override async ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        var triggerActivityId = context.WorkflowExecutionContext.TriggerActivityId;
        var triggerActivity = triggerActivityId != null ? Activities.FirstOrDefault(x => x.Id == triggerActivityId) : default;
        var startActivity = triggerActivity ?? Start ?? Activities.FirstOrDefault();

        if (startActivity == null!)
        {
            await context.CompleteActivityAsync();
            return;
        }

        await context.ScheduleActivityAsync(startActivity);
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

        // If specific outcomes were provided by the completed activity, use them to find the connection to the next activity.
        Func<Connection, bool> outboundConnectionsQuery = signal.Result is Outcomes outcomes
            ? connection => connection.Source.Activity == completedActivity && outcomes.Names.Contains(connection.Source.Port)
            : connection => connection.Source.Activity == completedActivity;

        var outboundConnections = Connections.Where(outboundConnectionsQuery).ToList();
        var children = outboundConnections.Select(x => x.Target.Activity).ToList();
        var scope = flowchartActivityExecutionContext.GetProperty(ScopeProperty, () => new FlowScope());

        scope.RegisterActivityExecution(completedActivity);

        if (children.Any())
        {
            scope.AddActivities(children);

            // Schedule each child, but only if all of its left inbound activities have already executed.
            foreach (var activity in children)
            {
                var inboundActivities = Connections.LeftInboundActivities(activity).ToList();

                // If the completed activity is not part of the left inbound path, always allow its children to be scheduled.
                if (!inboundActivities.Contains(completedActivity))
                {
                    await flowchartActivityExecutionContext.ScheduleActivityAsync(activity);
                    continue;
                }

                // If the activity is anything but a join activity, only schedule it if all of its left-inbound activities have executed, effectively implementing a "wait all" join. 
                if (activity is not IJoinNode)
                {
                    var executionCount = scope.GetExecutionCount(activity);
                    var haveInboundActivitiesExecuted = inboundActivities.All(x => scope.GetExecutionCount(x) > executionCount);

                    if (haveInboundActivitiesExecuted)
                        await flowchartActivityExecutionContext.ScheduleActivityAsync(activity);
                }
                else
                {
                    await flowchartActivityExecutionContext.ScheduleActivityAsync(activity);
                }
            }
        }

        if (!children.Any())
        {
            var workflowExecutionContext = context.ReceiverActivityExecutionContext.WorkflowExecutionContext;

            // If there is no pending work, complete the flowchart activity.
            var hasPendingWork = HasPendingWork(workflowExecutionContext);

            if (!hasPendingWork)
                await flowchartActivityExecutionContext.CompleteActivityAsync();
        }

        flowchartActivityExecutionContext.SetProperty(ScopeProperty, scope);
        context.StopPropagation();
    }

    /// <summary>
    /// Checks if there is any pending work for the flowchart.
    /// </summary>
    private bool HasPendingWork(WorkflowExecutionContext workflowExecutionContext)
    {
        var scheduler = workflowExecutionContext.Scheduler;
        var workItems = scheduler.List();
        var activityNodeIds = workItems.Select(x => x.ActivityId).ToList();
        var flowchartChildNodeIds = Activities.Select(workflowExecutionContext.FindNodeByActivity).Select(x => x.NodeId).ToList();
        var hasPendingWork = activityNodeIds.Intersect(flowchartChildNodeIds).Any();
        return hasPendingWork;
    }

    private bool HaveAllLeafsExecuted(FlowScope scope)
    {
        var leafs = Activities.Where(x => Connections.All(y => y.Source != x)).ToList();
        var leafsExecuted = leafs.All(x => scope.GetExecutionCount(x) > 0);
        return leafsExecuted;
    }
}