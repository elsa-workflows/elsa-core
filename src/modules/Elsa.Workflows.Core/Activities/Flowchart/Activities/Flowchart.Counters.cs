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
    private const string BackwardConnectionActivityInput = "BackwardConnection";

    private async ValueTask OnChildCompletedCounterBasedLogicAsync(ActivityCompletedContext context)
    {
        var flowchartContext = context.TargetContext;
        var completedActivityContext = context.ChildContext;
        var completedActivity = completedActivityContext.Activity;
        var result = context.Result;

        if (flowchartContext.Activity != this)
        {
            throw new Exception("Target context activity must be this flowchart");
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

        // Determine the outcomes from the completed activity
        var outcomes = result is Outcomes o ? o : Outcomes.Default;

        // Schedule the outbound activities
        var flowGraph = flowchartContext.GetFlowGraph();
        var flowScope = GetFlowScope(flowchartContext);
        var completedActivityExecutedByBackwardConnection = completedActivityContext.ActivityInput.GetValueOrDefault<bool>(BackwardConnectionActivityInput);
        bool hasScheduledActivity = await ScheduleOutboundActivitiesAsync(flowGraph, flowScope, flowchartContext, completedActivity, outcomes, completedActivityExecutedByBackwardConnection);

        // If there are not any outbound connections, complete the flowchart activity if there is no other pending work
        if (!hasScheduledActivity)
        {
            await CompleteIfNoPendingWorkAsync(flowchartContext);
        }
    }

    private FlowScope GetFlowScope(ActivityExecutionContext context)
    {
        return context.GetProperty(ScopeProperty, () => new FlowScope());
    }

    /// <summary>
    /// Schedules outbound activities based on the flowchart's structure and execution state.
    /// This method determines whether an activity should be scheduled based on visited connections,
    /// forward traversal rules, and backward connections.
    /// </summary>
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
            throw new Exception($"Activity {activity.Id} is not reachable from the flowchart graph. Unable to schedule it's outbound activities.");
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

            // Determine scheduling strategy based on connection type
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
            throw new Exception($"Invalid backward connection: Every path from the source ('{outboundConnection.Source.Activity.Id}') must go through the target ('{outboundConnection.Target.Activity.Id}') when tracing back to the start.");
        }

        var scheduleWorkOptions = new ScheduleWorkOptions
        {
            CompletionCallback = OnChildCompletedCounterBasedLogicAsync,
            Input = new Dictionary<string, object>()
            {
                {
                    BackwardConnectionActivityInput, true
                }
            }
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
            await flowchartContext.ScheduleActivityAsync(outboundActivity, OnChildCompletedCounterBasedLogicAsync);
            return true;
        }
        else
        {
            // Propagate skipped connections by scheduling with Outcomes.Empty
            return await ScheduleOutboundActivitiesAsync(flowGraph, flowScope, flowchartContext, outboundActivity, Outcomes.Empty);
        }
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

        if (joinContext is not { Status: ActivityStatus.Running })
        {
            var scheduleWorkOptions = new ScheduleWorkOptions
            {
                CompletionCallback = OnChildCompletedCounterBasedLogicAsync,
                ExistingActivityExecutionContext = joinContext
            };
            await flowchartContext.ScheduleActivityAsync(outboundActivity, scheduleWorkOptions);
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool CanWaitAllProceed(ActivityExecutionContext context)
    {
        var flowchartContext = context.ParentActivityExecutionContext!;
        var flowchart = (Flowchart)flowchartContext.Activity;
        var flowGraph = flowchartContext.GetFlowGraph();
        var flowScope = flowchart.GetFlowScope(flowchartContext);
        var activity = context.Activity;

        return flowScope.AllInboundConnectionsVisited(flowGraph, activity);
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
            foreach (var activity in outboundActivities)
                await flowchartContext.ScheduleActivityAsync(activity, OnChildCompletedCounterBasedLogicAsync);
        }
    }

    private async ValueTask OnCounterFlowActivityCanceledAsync(CancelSignal signal, SignalContext context)
    {
        var flowchartContext = context.ReceiverActivityExecutionContext;
        await CompleteIfNoPendingWorkAsync(flowchartContext);
        var flowchart = (Flowchart)flowchartContext.Activity;
        var flowGraph = flowchartContext.GetFlowGraph();
        var flowScope = flowchart.GetFlowScope(flowchartContext);

        // Propagate canceled connections visited count by scheduling with Outcomes.Empty
        await flowchart.ScheduleOutboundActivitiesAsync(flowGraph, flowScope, flowchartContext, context.SenderActivityExecutionContext.Activity, Outcomes.Empty);
    }
}