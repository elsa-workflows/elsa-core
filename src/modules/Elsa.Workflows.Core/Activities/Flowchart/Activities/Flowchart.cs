using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Contracts;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Options;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

/// <summary>
/// A flowchart consists of a collection of activities and connections between them.
/// </summary>
[Activity("Elsa", "Flow", "A flowchart is a collection of activities and connections between them.")]
[Browsable(false)]
public class Flowchart : Container
{
    private const string ScopeProperty = "FlowScope";
    private const string GraphTransientProperty = "FlowGraph";
    private const string BackwardConnectionActivityInput = "BackwardConnection";

    /// <inheritdoc />
    public Flowchart([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        OnSignalReceived<ScheduleActivityOutcomes>(OnScheduleOutcomesAsync);
        OnSignalReceived<ScheduleChildActivity>(OnScheduleChildActivityAsync);
        OnSignalReceived<CancelSignal>(OnActivityCanceledAsync);
    }

    /// <summary>
    /// The activity to execute when the flowchart starts.
    /// </summary>
    [Port] [Browsable(false)] public IActivity? Start { get; set; }

    /// <summary>
    /// A list of connections between activities.
    /// </summary>
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();

    /// <inheritdoc />
    protected override async ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        var startActivity = GetStartActivity(context);

        if (startActivity == null)
        {
            // Nothing else to execute.
            await context.CompleteActivityAsync();
            return;
        }

        // Schedule the start activity.
        await context.ScheduleActivityAsync(startActivity, OnChildCompletedAsync);
    }

    private IActivity? GetStartActivity(ActivityExecutionContext context)
    {
        // If there's a trigger that triggered this workflow, use that.
        var triggerActivityId = context.WorkflowExecutionContext.TriggerActivityId;
        var triggerActivity = triggerActivityId != null ? Activities.FirstOrDefault(x => x.Id == triggerActivityId) : null;

        if (triggerActivity != null)
            return triggerActivity;

        // If an explicit Start activity was provided, use that.
        if (Start != null)
            return Start;

        // If there is a Start activity on the flowchart, use that.
        var startActivity = Activities.FirstOrDefault(x => x is Start);

        if (startActivity != null)
            return startActivity;

        // If there's an activity marked as "Can Start Workflow", use that.
        var canStartWorkflowActivity = Activities.FirstOrDefault(x => x.GetCanStartWorkflow());

        if (canStartWorkflowActivity != null)
            return canStartWorkflowActivity;

        // If there is a single activity that has no inbound connections, use that.
        var root = GetRootActivity();

        if (root != null)
            return root;

        // If no start activity found, return the first activity.
        return Activities.FirstOrDefault();
    }

    /// <summary>
    /// Checks if there is any pending work for the flowchart.
    /// </summary>
    private bool HasPendingWork(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var activityIds = Activities.Select(x => x.Id).ToList();
        var children = context.Children;
        var hasRunningActivityInstances = children.Where(x => activityIds.Contains(x.Activity.Id)).Any(x => x.Status == ActivityStatus.Running);

        var hasPendingWork = workflowExecutionContext.Scheduler.List().Any(workItem =>
        {
            var ownerInstanceId = workItem.Owner?.Id;

            if (ownerInstanceId == null)
                return false;

            if (ownerInstanceId == context.Id)
                return true;

            var ownerContext = context.WorkflowExecutionContext.ActivityExecutionContexts.First(x => x.Id == ownerInstanceId);
            var ancestors = ownerContext.GetAncestors().ToList();

            return ancestors.Any(x => x == context);
        });

        return hasRunningActivityInstances || hasPendingWork;
    }

    private IActivity? GetRootActivity()
    {
        // Get the first activity that has no inbound connections.
        var query =
            from activity in Activities
            let inboundConnections = Connections.Any(x => x.Target.Activity == activity)
            where !inboundConnections
            select activity;

        var rootActivity = query.FirstOrDefault();
        return rootActivity;
    }

    private FlowGraph GetFlowGraph(ActivityExecutionContext context)
    {
        // Store in TransientProperties so FlowChart is not persisted in WorkflowState 
        return context.TransientProperties.GetOrAdd(GraphTransientProperty, () => new FlowGraph(Connections, GetStartActivity(context)));
    }

    private FlowScope GetFlowScope(ActivityExecutionContext context)
    {
        return context.GetProperty(ScopeProperty, () => new FlowScope());
    }

    private async ValueTask OnChildCompletedAsync(ActivityCompletedContext context)
    {
        var flowchartContext = context.TargetContext;
        var completedActivityContext = context.ChildContext;
        var completedActivity = completedActivityContext.Activity;
        var result = context.Result;

        if (flowchartContext.Activity != this)
        {
            throw new("Target context activity must be this flowchart");
        }

        // If the completed activity's status is anything but "Completed", do not schedule its outbound activities.
        if (completedActivityContext.Status != ActivityStatus.Completed)
        {
            return;
        }

        // If the complete activity is a terminal node, complete the flowchart immediately.
        if (completedActivity is ITerminalNode)
        {
            await flowchartContext.CompleteActivityAsync();
            return;
        }

        // Determine the outcomes from the completed activity.
        var outcomes = result is Outcomes o ? o : Outcomes.Default;

        // Schedule the outbound activities
        var flowGraph = GetFlowGraph(flowchartContext);
        var flowScope = GetFlowScope(flowchartContext);
        var completedActivityExecutedByBackwardConnection = completedActivityContext.ActivityInput.GetValueOrDefault<bool>(BackwardConnectionActivityInput);
        var hasScheduledActivity = await ScheduleOutboundActivitiesAsync(flowGraph, flowScope, flowchartContext, completedActivity, outcomes, completedActivityExecutedByBackwardConnection);

        // If there are not any outbound connections, complete the flowchart activity if there is no other pending work.
        if (!hasScheduledActivity)
        {
            await CompleteIfNoPendingWorkAsync(flowchartContext);
        }
    }

    /// <summary>
    /// Schedules outbound activities based on the flowchart's structure and execution state.
    /// This method determines whether an activity should be scheduled based on visited connections,
    /// forward traversal rules, and backward connections.
    /// </summary>
    /// <param name="flowchart">The flowchart containing the activities.</param>
    /// <param name="flowGraph">The graph representation of the flowchart.</param>
    /// <param name="flowScope">Tracks activity and connection visits.</param>
    /// <param name="flowchartContext">The execution context of the flowchart.</param>
    /// <param name="activity">The current activity being processed.</param>
    /// <param name="outcomes">The outcomes that determine which connections were followed.</param>
    /// <param name="completedActivityExecutedByBackwardConnection">Indicates if the completed activity was executed due to a backward connection.</param>
    /// <returns>True if at least one activity was scheduled; otherwise, false.</returns>
    private async ValueTask<bool> ScheduleOutboundActivitiesAsync(FlowGraph flowGraph, FlowScope flowScope, ActivityExecutionContext flowchartContext, IActivity activity, Outcomes outcomes, bool completedActivityExecutedByBackwardConnection = false)
    {
        var hasScheduledActivity = false;

        // Check if the activity is dangling (i.e., it is not reachable from the flowchart graph)
        if (flowGraph.IsDanglingActivity(activity))
        {
            throw new($"Activity {activity.Id} is not reachable from the flowchart graph. Unable to schedule it's outbound activities.");
        }

        // Register the activity as visited unless it was executed due to a backward connection
        if (!completedActivityExecutedByBackwardConnection)
        {
            flowScope.RegisterActivityVisit(activity);
        }
        
        var outboundConnections = flowGraph.GetOutboundConnections(activity);

        // Register the outbound connections as visited.
        foreach (var outboundConnection in outboundConnections)
        {
            var connectionFollowed = outcomes.Names.Contains(outboundConnection.Source.Port);
            flowScope.RegisterConnectionVisit(outboundConnection, connectionFollowed);
        }

        // Process each outbound connection from the current activity
        foreach (var outboundConnection in outboundConnections)
        {
            var connectionFollowed = flowScope.GetConnectionLastVisitFollowed(outboundConnection);
            
            if(!connectionFollowed)
                continue; // Skip if the connection was not followed.
            
            var outboundActivity = outboundConnection.Target.Activity;

            // Determine the scheduling strategy based on connection-type.
            if (flowGraph.IsBackwardConnection(outboundConnection, out var backwardConnectionIsValid))
            {
                hasScheduledActivity |= await ScheduleBackwardConnectionActivityAsync(flowGraph, flowchartContext, outboundConnection, outboundActivity, connectionFollowed, backwardConnectionIsValid);
            }
            else if (outboundActivity is not IJoinNode)
            {
                hasScheduledActivity |= await ScheduleNonJoinActivityAsync(flowGraph, flowScope, flowchartContext, outboundActivity);
            }
            else
            {
                hasScheduledActivity |= await ScheduleJoinActivityAsync(flowGraph, flowScope, flowchartContext, outboundConnection, outboundActivity);
            }
        }
        return hasScheduledActivity;
    }

    /// <summary>
    /// Schedules an outbound activity that originates from a backward connection.
    /// </summary>
    private async ValueTask<bool> ScheduleBackwardConnectionActivityAsync(FlowGraph flowGraph, ActivityExecutionContext flowchartContext, Connection outboundConnection, IActivity outboundActivity, bool connectionFollowed, bool backwardConnectionIsValid)
    {
        if (!connectionFollowed)
        {
            return false;
        }

        if (!backwardConnectionIsValid)
        {
            throw new($"Invalid backward connection: Every path from the source ('{outboundConnection.Source.Activity.Id}') must go through the target ('{outboundConnection.Target.Activity.Id}') when tracing back to the start.");
        }

        var scheduleWorkOptions = new ScheduleWorkOptions
        {
            CompletionCallback = OnChildCompletedAsync,
            Input = new Dictionary<string, object>() { { BackwardConnectionActivityInput, true } }
        };

        await flowchartContext.ScheduleActivityAsync(outboundActivity, scheduleWorkOptions);
        return true;
    }

    /// <summary>
    /// Schedules a non-join activity if all its forward inbound connections have been visited.
    /// </summary>
    private async ValueTask<bool> ScheduleNonJoinActivityAsync(FlowGraph flowGraph, FlowScope flowScope, ActivityExecutionContext flowchartContext, IActivity outboundActivity)
    {
        if (!flowScope.AllInboundConnectionsVisited(flowGraph, outboundActivity))
        {
            return false;
        }

        if (flowScope.HasFollowedInboundConnection(flowGraph, outboundActivity))
        {
            await flowchartContext.ScheduleActivityAsync(outboundActivity, OnChildCompletedAsync);
            return true;
        }

        // Propagate skipped connections by scheduling with Outcomes.Empty
        return await ScheduleOutboundActivitiesAsync(flowGraph, flowScope, flowchartContext, outboundActivity, Outcomes.Empty);
    }

    /// <summary>
    /// Schedules a join activity based on inbound connection statuses.
    /// </summary>
    private async ValueTask<bool> ScheduleJoinActivityAsync(FlowGraph flowGraph, FlowScope flowScope, ActivityExecutionContext flowchartContext, Connection outboundConnection, IActivity outboundActivity)
    {
        // Ignore the connection if the join activity has already completed (JoinAny scenario)
        if (flowScope.ShouldIgnoreConnection(outboundConnection, outboundActivity))
        {
            return false;
        }

        // Schedule the join activity only if at least one inbound connection was followed
        if (!flowScope.HasFollowedInboundConnection(flowGraph, outboundActivity))
        {
            if (flowScope.AllInboundConnectionsVisited(flowGraph, outboundActivity))
            {
                // Propagate skipped connections by scheduling with Outcomes.Empty
                return await ScheduleOutboundActivitiesAsync(flowGraph, flowScope, flowchartContext, outboundActivity, Outcomes.Empty);
            }
            return false;
        }

        // Check for an existing execution context for the join activity
        var joinContext = flowchartContext.WorkflowExecutionContext.ActivityExecutionContexts.LastOrDefault(x =>
            x.ParentActivityExecutionContext == flowchartContext &&
            x.Activity == outboundActivity &&
            x.Status is ActivityStatus.Pending or ActivityStatus.Running);

        // If the join activity was already scheduled, do not schedule it again
        if (joinContext == null)
        {
            var activityScheduled = flowchartContext.WorkflowExecutionContext.Scheduler.List().Any(workItem => workItem.Owner == flowchartContext && workItem.Activity == outboundActivity);
            if (activityScheduled)
            {
                return true;
            }
        }

        var scheduleWorkOptions = new ScheduleWorkOptions
        {
            CompletionCallback = OnChildCompletedAsync,
            ExistingActivityExecutionContext = joinContext
        };
        await flowchartContext.ScheduleActivityAsync(outboundActivity, scheduleWorkOptions);
        return true;
    }

    public static bool CanWaitAllProceed(ActivityExecutionContext context)
    {
        var flowchartContext = context.ParentActivityExecutionContext!;
        var flowchart = (Flowchart)flowchartContext.Activity;
        var flowGraph = flowchart.GetFlowGraph(flowchartContext);
        var flowScope = flowchart.GetFlowScope(flowchartContext);
        var activity = context.Activity;

        return flowScope.AllInboundConnectionsVisited(flowGraph, activity);
    }

    public static async void CancelAncestorActivatesAsync(ActivityExecutionContext context)
    {
        var flowchartContext = context.ParentActivityExecutionContext!;
        var flowchart = (Flowchart)flowchartContext.Activity;
        var flowGraph = flowchart.GetFlowGraph(flowchartContext);
        var ancestorActivities = flowGraph.GetAncestorActivities(context.Activity);
        var inboundActivityExecutionContexts = context.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => ancestorActivities.Contains(x.Activity) && x.ParentActivityExecutionContext == flowchartContext).ToList();

        // Cancel each ancestor activity.
        foreach (var activityExecutionContext in inboundActivityExecutionContexts)
        {
            await activityExecutionContext.CancelActivityAsync();
        }
    }

    private async Task CompleteIfNoPendingWorkAsync(ActivityExecutionContext context)
    {
        var hasPendingWork = HasPendingWork(context);

        if (!hasPendingWork)
        {
            var hasFaultedActivities = context.Children.Any(x => x.Status == ActivityStatus.Faulted);

            if (!hasFaultedActivities)
            {
                await context.CompleteActivityAsync();
            }
        }
    }

    private async ValueTask OnScheduleOutcomesAsync(ScheduleActivityOutcomes signal, SignalContext context)
    {
        var flowchartContext = context.ReceiverActivityExecutionContext;
        var schedulingActivityContext = context.SenderActivityExecutionContext;
        var schedulingActivity = schedulingActivityContext.Activity;
        var outcomes = signal.Outcomes;
        var outboundConnections = Connections.Where(connection => connection.Source.Activity == schedulingActivity && outcomes.Contains(connection.Source.Port!)).ToList();
        var outboundActivities = outboundConnections.Select(x => x.Target.Activity).ToList();

        if (outboundActivities.Any())
        {
            // Schedule each child.
            foreach (var activity in outboundActivities) await flowchartContext.ScheduleActivityAsync(activity, OnChildCompletedAsync);
        }
    }

    private async ValueTask OnScheduleChildActivityAsync(ScheduleChildActivity signal, SignalContext context)
    {
        var flowchartContext = context.ReceiverActivityExecutionContext;
        var activity = signal.Activity;
        var activityExecutionContext = signal.ActivityExecutionContext;

        if (activityExecutionContext != null)
        {
            await flowchartContext.ScheduleActivityAsync(activityExecutionContext.Activity, new()
            {
                ExistingActivityExecutionContext = activityExecutionContext,
                CompletionCallback = OnChildCompletedAsync,
                Input = signal.Input
            });
        }
        else
        {
            await flowchartContext.ScheduleActivityAsync(activity, new()
            {
                CompletionCallback = OnChildCompletedAsync,
                Input = signal.Input
            });
        }
    }

    private async ValueTask OnActivityCanceledAsync(CancelSignal signal, SignalContext context)
    {
        await CompleteIfNoPendingWorkAsync(context.ReceiverActivityExecutionContext);
    }
}