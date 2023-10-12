using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Contracts;
using Elsa.Workflows.Core.Activities.Flowchart.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Core.Signals;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

/// <summary>
/// A flowchart consists of a collection of activities and connections between them.
/// </summary>
[Activity("Elsa", "Flow", "A flowchart is a collection of activities and connections between them.")]
[Browsable(false)]
public class Flowchart : Container
{
    internal const string ScopeProperty = "Scope";
    internal const string BranchMonitorsProperty = "BranchMonitoring";

    /// <inheritdoc />
    public Flowchart([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        OnSignalReceived<ScheduleActivityOutcomes>(OnScheduleOutcomesAsync);
        OnSignalReceived<ScheduleChildActivity>(OnScheduleChildActivityAsync);
    }

    /// <summary>
    /// The activity to execute when the flowchart starts.
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
        var logger = context.GetRequiredService<ILogger<Flowchart>>();

        var startActivity = GetStartActivity(context);

        if (startActivity == null)
        {
            // Nothing else to execute.
            logger.LogDebug("No start activity found. Completing flowchart");
            await context.CompleteActivityAsync();
            return;
        }

        // Schedule the start activity.
        logger.LogDebug("Scheduling activity: {StartActivityId}", startActivity.Id);
        await context.ScheduleActivityAsync(startActivity, OnChildCompletedAsync);
    }

    private IActivity? GetStartActivity(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<Flowchart>>();

        logger.LogDebug("Looking for start activity...");
        
        // If there's a trigger that triggered this workflow, use that.
        var triggerActivityId = context.WorkflowExecutionContext.TriggerActivityId;
        var triggerActivity = triggerActivityId != null ? Activities.FirstOrDefault(x => x.Id == triggerActivityId) : default;

        if (triggerActivity != null)
        {
            logger.LogDebug("Found trigger activity: {TriggerActivityId}", triggerActivityId);
            return triggerActivity;
        }

        // If an explicit Start activity was provided, use that.
        if (Start != null)
        {
            logger.LogDebug("An explicit start activity was provided: {StartActivityId}", Start.Id);
            return Start;
        }

        // If there is a Start activity on the flowchart, use that.
        var startActivity = Activities.FirstOrDefault(x => x is Start);

        if (startActivity != null)
        {
            logger.LogDebug("A Start activity was found: {StartActivityId}", startActivity.Id);
            return startActivity;
        }
        
        // If there's an activity marked as "Can Start Workflow", use that.
        var canStartWorkflowActivity = Activities.FirstOrDefault(x => x.GetCanStartWorkflow());
        
        if(canStartWorkflowActivity != null)
        {
            logger.LogDebug("An activity marked as 'Can Start Workflow' was found: {CanStartWorkflowActivityId}", canStartWorkflowActivity.Id);
            return canStartWorkflowActivity;
        }

        // If there is a single activity that has no inbound connections, use that.
        var root = GetRootActivity();

        if (root != null)
        {
            logger.LogDebug("Found a single activity with no inbound connections: {ActivityId}", root.Id);
            return root;
        }

        // If no start activity found, return the first activity.
        logger.LogDebug("No start activity found. Using the first activity");
        return Activities.FirstOrDefault();
    }

    /// <summary>
    /// Checks if there is any pending work for the flowchart.
    /// </summary>
    private bool HasPendingWork(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var activityIds = Activities.Select(x => x.Id).ToList();
        var descendantContexts = context.GetDescendents().Where(x => x.ParentActivityExecutionContext == context).ToList();
        var activityExecutionContexts = descendantContexts.Where(x => activityIds.Contains(x.Activity.Id)).ToList();

        var hasPendingWork = workflowExecutionContext.Scheduler.List().Any(workItem =>
        {
            var ownerInstanceId = workItem.Owner?.Id;

            if (ownerInstanceId == null)
                return false;

            var ownerContext = context.WorkflowExecutionContext.ActivityExecutionContexts.First(x => x.Id == ownerInstanceId);
            var ancestors = ownerContext.GetAncestors().ToList();

            return ancestors.Any(x => x == context);
        });

        var hasRunningActivityInstances = activityExecutionContexts.Any(x => x.Status == ActivityStatus.Running);

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

    private async ValueTask OnChildCompletedAsync(ActivityCompletedContext context)
    {
        var logger = context.GetRequiredService<ILogger<Flowchart>>();
        var flowchartContext = context.TargetContext;
        var completedActivityContext = context.ChildContext;
        var completedActivity = completedActivityContext.Activity;
        var result = context.Result;

        logger.LogDebug("Child activity {ActivityId} completed with status {ActivityStatus}", completedActivity.Id, completedActivityContext.Status);

        // If the complete activity's status is anything but "Completed", do not schedule its outbound activities.
        var scheduleChildren = completedActivityContext.Status == ActivityStatus.Completed;
        var outcomeNames = result is Outcomes outcomes ? outcomes.Names : new[] { default(string), "Done" };

        // Only query the outbound connections if the completed activity wasn't already completed.
        var outboundConnections = Connections.Where(connection => connection.Source.Activity == completedActivity && outcomeNames.Contains(connection.Source.Port)).ToList();
        var children = outboundConnections.Select(x => x.Target.Activity).ToList();
        var scope = flowchartContext.GetProperty(ScopeProperty, () => new FlowScope());

        scope.RegisterActivityExecution(completedActivity);

        // If the complete activity is a terminal node, complete the flowchart immediately.
        if (completedActivity is ITerminalNode)
        {
            logger.LogDebug("Completed activity {ActivityId} is a terminal activity. Completing flowchart", completedActivity.Id);
            await flowchartContext.CompleteActivityAsync();
        }
        else if (scheduleChildren)
        {
            if (children.Any())
            {
                if (children.Count == 1)
                    logger.LogDebug("Found 1 child for activity {ActivityId}: {ChildActivityId}", completedActivity.Id, children.First().Id);
                else
                    logger.LogDebug("Found {Count} children for activity {ActivityId}: {ChildActivityIds}", children.Count, completedActivity.Id, children.Select(x => x.Id).ToList());

                scope.AddActivities(children);

                // Schedule each child, but only if all of its left inbound activities have already executed.
                foreach (var activity in children)
                {
                    var inboundActivities = Connections.LeftInboundActivities(activity).ToList();

                    // If the completed activity is not part of the left inbound path, always allow its children to be scheduled.
                    if (!inboundActivities.Contains(completedActivity))
                    {
                        logger.LogDebug("Scheduling child activity {ChildActivityId}", activity.Id);
                        await flowchartContext.ScheduleActivityAsync(activity, OnChildCompletedAsync);
                        continue;
                    }

                    // If the activity is anything but a join activity, only schedule it if all of its left-inbound activities have executed, effectively implementing a "wait all" join. 
                    if (activity is not IJoinNode)
                    {
                        var executionCount = scope.GetExecutionCount(activity);
                        var haveInboundActivitiesExecuted = inboundActivities.All(x => scope.GetExecutionCount(x) > executionCount);

                        if (haveInboundActivitiesExecuted)
                        {
                            logger.LogDebug("Scheduling child activity {ChildActivityId}", activity.Id);
                            await flowchartContext.ScheduleActivityAsync(activity, OnChildCompletedAsync);
                        }
                    }
                    else
                    {
                        // Select an existing activity execution context for this activity, if any.
                        var joinContext = flowchartContext.WorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.ParentActivityExecutionContext == flowchartContext && x.Activity == activity);
                        var scheduleWorkOptions = new ScheduleWorkOptions
                        {
                            CompletionCallback = OnChildCompletedAsync,
                            ExistingActivityExecutionContext = joinContext,
                            PreventDuplicateScheduling = true
                        };

                        if (joinContext != null)
                            logger.LogDebug("Next activity {ChildActivityId} is a join activity. Attaching to existing context {JoinContext}", activity.Id, joinContext.Id);
                        else
                            logger.LogDebug("Next activity {ChildActivityId} is a join activity", activity.Id);

                        logger.LogDebug("Scheduling child activity {ChildActivityId}", activity.Id);
                        await flowchartContext.ScheduleActivityAsync(activity, scheduleWorkOptions);
                    }
                }
            }

            if (!children.Any())
            {
                logger.LogDebug("No children found for activity {ActivityId}", completedActivity.Id);

                // If there is no pending work, complete the flowchart activity.
                var hasPendingWork = HasPendingWork(flowchartContext);

                if (!hasPendingWork)
                {
                    logger.LogDebug("No pending work found");
                    var hasFaultedActivities = flowchartContext.GetActiveChildren().Any(x => x.Status == ActivityStatus.Faulted);

                    if (!hasFaultedActivities)
                    {
                        logger.LogDebug("No faulted activities found");
                        logger.LogDebug("Completing flowchart");
                        await flowchartContext.CompleteActivityAsync();
                    }
                }
            }
        }

        flowchartContext.SetProperty(ScopeProperty, scope);
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
            await flowchartContext.ScheduleActivityAsync(activityExecutionContext.Activity, new ScheduleWorkOptions
            {
                ExistingActivityExecutionContext = activityExecutionContext,
                CompletionCallback = OnChildCompletedAsync,
            });
        }
        else
        {
            await flowchartContext.ScheduleActivityAsync(activity, OnChildCompletedAsync);
        }
    }
}