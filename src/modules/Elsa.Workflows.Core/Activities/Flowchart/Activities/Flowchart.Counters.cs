using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Contracts;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

public partial class Flowchart
{
    private const string ScopeProperty = "FlowScope";
    private const string GraphTransientProperty = "FlowGraph";
    private const string BackwardConnectionActivityInput = "BackwardConnection";
    
    
    private async ValueTask OnChildCompletedCounterBasedLogicAsync(ActivityCompletedContext context)
    {
        var flowchartContext = context.TargetContext;
        var completedActivityContext = context.ChildContext;
        var completedActivity = completedActivityContext.Activity;
        var result = context.Result;

        // Determine the outcomes from the completed activity
        var outcomes = result is Outcomes o ? o : Outcomes.Default;

        await ProcessChildCompletedAsync(flowchartContext, completedActivity, completedActivityContext, outcomes);
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

        // Use HashSet for O(1) lookups
        var activityIds = new HashSet<string>(Activities.Select(x => x.Id));

        // Short circuit evaluation - check running instances first before more expensive scheduler check
        if (context.Children.Any(x => activityIds.Contains(x.Activity.Id) && x.Status == ActivityStatus.Running))
            return true;

        // Scheduler check - optimize to avoid repeated LINQ evaluations
        var scheduledItems = workflowExecutionContext.Scheduler.List().ToList();

        return scheduledItems.Any(workItem =>
        {
            var ownerInstanceId = workItem.Owner?.Id;

            if (ownerInstanceId == null)
                return false;

            if (ownerInstanceId == context.Id)
                return true;

            var ownerContext = workflowExecutionContext.ActivityExecutionContexts.First(x => x.Id == ownerInstanceId);
            return ownerContext.GetAncestors().Any(x => x == context);
        });
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

    private async ValueTask ProcessChildCompletedAsync(ActivityExecutionContext flowchartContext, IActivity completedActivity, ActivityExecutionContext completedActivityContext, Outcomes outcomes)
    {
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

        // Schedule the outbound activities
        var flowGraph = GetFlowGraph(flowchartContext);
        var flowScope = GetFlowScope(flowchartContext);
        var completedActivityExcecutedByBackwardConnection = completedActivityContext.ActivityInput.GetValueOrDefault<bool>(BackwardConnectionActivityInput);
        bool hasScheduledActivity = await MaybeScheduleOutboundActivitiesAsync(flowGraph, flowScope, flowchartContext, completedActivity, outcomes, OnChildCompletedAsync, completedActivityExcecutedByBackwardConnection);

        // If there are not any outbound connections, complete the flowchart activity if there is no other pending work
        if (!hasScheduledActivity)
        {
            await CompleteIfNoPendingWorkAsync(flowchartContext);
        }
    }

    /// <summary>
    /// Schedules outbound activities based on the flowchart's structure and execution state.
    /// This method determines whether an activity should be scheduled based on visited connections,
    /// forward traversal rules, and backward connections. If outcomes is Outcomes.Empty, it indicates 
    /// that the activity should be skipped - all outbound connections will be visited and treated as 
    /// not followed.
    /// </summary>
    /// <param name="flowGraph">The graph representation of the flowchart.</param>
    /// <param name="flowScope">Tracks activity and connection visits.</param>
    /// <param name="flowchartContext">The execution context of the flowchart.</param>
    /// <param name="activity">The current activity being processed.</param>
    /// <param name="outcomes">The outcomes that determine which connections were followed.</param>
    /// <param name="completionCallback">The callback to invoke upon activity completion.</param>
    /// <param name="completedActivityExecutedByBackwardConnection">Indicates if the completed activity 
    /// was executed due to a backward connection.</param>
    /// <returns>True if at least one activity was scheduled; otherwise, false.</returns>
    private static async ValueTask<bool> MaybeScheduleOutboundActivitiesAsync(FlowGraph flowGraph, FlowScope flowScope, ActivityExecutionContext flowchartContext, IActivity activity, Outcomes outcomes, ActivityCompletionCallback completionCallback, bool completedActivityExecutedByBackwardConnection = false)
    {
        bool hasScheduledActivity = false;

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

        // Process each outbound connection from the current activity
        foreach (var outboundConnection in flowGraph.GetOutboundConnections(activity))
        {
            var connectionFollowed = outcomes.Names.Contains(outboundConnection.Source.Port);
            flowScope.RegisterConnectionVisit(outboundConnection, connectionFollowed);
            var outboundActivity = outboundConnection.Target.Activity;

            // Determine the scheduling strategy based on connection-type.
            if (flowGraph.IsBackwardConnection(outboundConnection, out var backwardConnectionIsValid))
                // Backward connections are scheduled differently
                hasScheduledActivity |= await MaybeScheduleBackwardConnectionActivityAsync(flowGraph, flowchartContext, outboundConnection, outboundActivity, connectionFollowed, backwardConnectionIsValid, completionCallback);
            else
                hasScheduledActivity |= await MaybeScheduleOutboundActivityAsync(flowGraph, flowScope, flowchartContext, outboundConnection, outboundActivity, completionCallback);
        }

        return hasScheduledActivity;
    }

    /// <summary>
    /// Schedules an outbound activity that originates from a backward connection.
    /// </summary>
    private static async ValueTask<bool> MaybeScheduleBackwardConnectionActivityAsync(FlowGraph flowGraph, ActivityExecutionContext flowchartContext, Connection outboundConnection, IActivity outboundActivity, bool connectionFollowed, bool backwardConnectionIsValid, ActivityCompletionCallback completionCallback)
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
            CompletionCallback = completionCallback,
            Input = new Dictionary<string, object>() { { BackwardConnectionActivityInput, true } }
        };

        await flowchartContext.ScheduleActivityAsync(outboundActivity, scheduleWorkOptions);
        return true;
    }

    /// <summary>
    /// Determines the merge mode for a given outbound activity. If the outbound activity is a FlowJoin, it retrieves its configured 
    /// mode. Otherwise, it defaults to FlowJoinMode.WaitAllActive for implicit joins.
    /// </summary>
    private static async ValueTask<FlowJoinMode> GetMergeModeAsync(ActivityExecutionContext flowchartContext, IActivity outboundActivity)
    {
        if (outboundActivity is FlowJoin)
        {
            var outboundActivityExecutionContext = await flowchartContext.WorkflowExecutionContext.CreateActivityExecutionContextAsync(outboundActivity);
            return await outboundActivityExecutionContext.EvaluateInputPropertyAsync<FlowJoin, FlowJoinMode>(x => x.Mode);
        }
        else
        {
            // Implicit join case - treat as WaitAllActive
            return FlowJoinMode.WaitAllActive;
        }
    }

    /// <summary>
    /// Schedules a join activity based on inbound connection statuses.
    /// </summary>
    private static async ValueTask<bool> MaybeScheduleOutboundActivityAsync(FlowGraph flowGraph, FlowScope flowScope, ActivityExecutionContext flowchartContext, Connection outboundConnection, IActivity outboundActivity, ActivityCompletionCallback completionCallback)
    {
        FlowJoinMode mode = await GetMergeModeAsync(flowchartContext, outboundActivity);

        return mode switch
        {
            FlowJoinMode.WaitAll => await MaybeScheduleWaitAllActivityAsync(flowGraph, flowScope, flowchartContext, outboundActivity, completionCallback),
            FlowJoinMode.WaitAllActive => await MaybeScheduleWaitAllActiveActivityAsync(flowGraph, flowScope, flowchartContext, outboundActivity, completionCallback),
            FlowJoinMode.WaitAny => await MaybeScheduleWaitAnyActivityAsync(flowGraph, flowScope, flowchartContext, outboundConnection, outboundActivity, completionCallback),
            _ => throw new($"Unsupported FlowJoinMode: {mode}"),
        };
    }

    /// <summary>
    /// Determines whether to schedule an activity based on the FlowJoinMode.WaitAll behavior.
    /// If all inbound connections were visited, it checks if they were all followed to decide whether to schedule or skip the activity.
    /// </summary>
    private static async ValueTask<bool> MaybeScheduleWaitAllActivityAsync(FlowGraph flowGraph, FlowScope flowScope, ActivityExecutionContext flowchartContext, IActivity outboundActivity, ActivityCompletionCallback completionCallback)
    {
        if (!flowScope.AllInboundConnectionsVisited(flowGraph, outboundActivity))
            // Not all inbound connections have been visited yet; do not schedule anything yet.
            return false;

        if (flowScope.AllInboundConnectionsFollowed(flowGraph, outboundActivity))
            // All inbound connections were followed; schedule the outbound activity.
            return await ScheduleOutboundActivityAsync(flowchartContext, outboundActivity, completionCallback);
        else
            // No inbound connections were followed; skip the outbound activity.
            return await SkipOutboundActivityAsync(flowGraph, flowScope, flowchartContext, outboundActivity, completionCallback);
    }

    /// <summary>
    /// Determines whether to schedule an activity based on the FlowJoinMode.WaitAllActive behavior.
    /// If all inbound connections have been visited, it checks if any were followed to decide whether to schedule or skip the activity.
    /// </summary>
    private static async ValueTask<bool> MaybeScheduleWaitAllActiveActivityAsync(FlowGraph flowGraph, FlowScope flowScope, ActivityExecutionContext flowchartContext, IActivity outboundActivity, ActivityCompletionCallback completionCallback)
    {
        if (!flowScope.AllInboundConnectionsVisited(flowGraph, outboundActivity))
            // Not all inbound connections have been visited yet; do not schedule anything yet.
            return false;

        if (flowScope.AnyInboundConnectionsFollowed(flowGraph, outboundActivity))
            // At least one inbound connection was followed; schedule the outbound activity.
            return await ScheduleOutboundActivityAsync(flowchartContext, outboundActivity, completionCallback);
        else
            // No inbound connections were followed; skip the outbound activity.
            return await SkipOutboundActivityAsync(flowGraph, flowScope, flowchartContext, outboundActivity, completionCallback);
    }

    /// <summary>
    /// Determines whether to schedule an activity based on the FlowJoinMode.WaitAny behavior.
    /// If any inbound connection has been followed, it schedules the activity and cancels remaining inbound activities.
    /// If a subsequent inbound connection is followed after the activity has been scheduled, it ignores it.
    /// </summary>
    private static async ValueTask<bool> MaybeScheduleWaitAnyActivityAsync(FlowGraph flowGraph, FlowScope flowScope, ActivityExecutionContext flowchartContext, Connection outboundConnection, IActivity outboundActivity, ActivityCompletionCallback completionCallback)
    {
        if (flowScope.ShouldIgnoreConnection(outboundConnection, outboundActivity))
            // Ignore the connection if the outbound activity has already completed (JoinAny scenario)
            return false;

        if (flowchartContext.WorkflowExecutionContext.Scheduler.List().Any(workItem => workItem.Owner == flowchartContext && workItem.Activity == outboundActivity))
            // Ignore the connection if the outbound activity is already scheduled
            return false;

        if (flowScope.AnyInboundConnectionsFollowed(flowGraph, outboundActivity))
        {
            // An inbound connection has been followed; cancel remaining inbound activities
            await CancelRemainingInboundActivitiesAsync(flowchartContext, outboundActivity);

            // This is the first inbound connection followed; schedule the outbound activity
            return await ScheduleOutboundActivityAsync(flowchartContext, outboundActivity, completionCallback);
        }

        if (flowScope.AllInboundConnectionsVisited(flowGraph, outboundActivity))
            // All inbound connections have been visited without any being followed; skip the outbound activity
            return await SkipOutboundActivityAsync(flowGraph, flowScope, flowchartContext, outboundActivity, completionCallback);

        // No inbound connections have been followed yet; do not schedule anything yet.
        return false;
    }

    /// <summary>
    /// Schedules the outbound activity.
    /// </summary>
    private static async ValueTask<bool> ScheduleOutboundActivityAsync(ActivityExecutionContext flowchartContext, IActivity outboundActivity, ActivityCompletionCallback completionCallback)
    {
        await flowchartContext.ScheduleActivityAsync(outboundActivity, completionCallback);
        return true;
    }

    /// <summary>
    /// Skips the outbound activity by propagating skipped connections.
    /// </summary>
    private static async ValueTask<bool> SkipOutboundActivityAsync(FlowGraph flowGraph, FlowScope flowScope, ActivityExecutionContext flowchartContext, IActivity outboundActivity, ActivityCompletionCallback completionCallback)
    {
        return await MaybeScheduleOutboundActivitiesAsync(flowGraph, flowScope, flowchartContext, outboundActivity, Outcomes.Empty, completionCallback);
    }

    private static async ValueTask CancelRemainingInboundActivitiesAsync(ActivityExecutionContext flowchartContext, IActivity outboundActivity)
    {
        var flowchart = (Flowchart)flowchartContext.Activity;
        var flowGraph = flowchart.GetFlowGraph(flowchartContext);
        var ancestorActivities = flowGraph.GetAncestorActivities(outboundActivity);
        var inboundActivityExecutionContexts = flowchartContext.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => ancestorActivities.Contains(x.Activity) && x.ParentActivityExecutionContext == flowchartContext).ToList();

        // Cancel each ancestor activity.
        foreach (var activityExecutionContext in inboundActivityExecutionContexts)
        {
            await activityExecutionContext.CancelActivityAsync();
        }
    }

    private async ValueTask OnScheduleOutcomesAsync(ScheduleActivityOutcomes signal, SignalContext context)
    {
        var flowchartContext = context.ReceiverActivityExecutionContext;
        var schedulingActivityContext = context.SenderActivityExecutionContext;
        var schedulingActivity = schedulingActivityContext.Activity;
        var outcomes = new Outcomes(signal.Outcomes);
    }

    private async ValueTask OnCounterFlowActivityCanceledAsync(CancelSignal signal, SignalContext context)
    {
        var flowchartContext = context.ReceiverActivityExecutionContext;
        await CompleteIfNoPendingWorkAsync(flowchartContext);
        var flowchart = (Flowchart)flowchartContext.Activity;
        var flowGraph = flowchartContext.GetFlowGraph();
        var flowScope = flowchart.GetFlowScope(flowchartContext);

        // Propagate canceled connections visited count by scheduling with Outcomes.Empty
        await MaybeScheduleOutboundActivitiesAsync(flowGraph, flowScope, flowchartContext, context.SenderActivityExecutionContext.Activity, Outcomes.Empty, OnChildCompletedAsync);
    }
}