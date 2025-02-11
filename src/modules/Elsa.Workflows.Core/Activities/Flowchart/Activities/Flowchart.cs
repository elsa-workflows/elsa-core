using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Contracts;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Options;
using Elsa.Workflows.Signals;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

/// <summary>
/// A flowchart consists of a collection of activities and connections between them.
/// </summary>
[Activity("Elsa", "Flow", "A flowchart is a collection of activities and connections between them.")]
[Browsable(false)]
public class Flowchart : Container
{
    public const string ScopeProperty = "Scope";

    /// <inheritdoc />
    public Flowchart([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
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
        var triggerActivity = triggerActivityId != null ? Activities.FirstOrDefault(x => x.Id == triggerActivityId) : default;

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

    private async ValueTask OnChildCompletedAsync(ActivityCompletedContext context)
    {
        var logger = context.GetRequiredService<ILogger<Flowchart>>();
        var flowchartContext = context.TargetContext;
        var completedActivityContext = context.ChildContext;
        var completedActivity = completedActivityContext.Activity;
        var result = context.Result;

        // If the complete activity's status is anything but "Completed", do not schedule its outbound activities.
        var scheduleChildren = completedActivityContext.Status == ActivityStatus.Completed;
        var outcomeNames = result is Outcomes outcomes
            ? outcomes.Names
            : [null!, "Done"];

        // Only query the outbound connections if the completed activity wasn't already completed.
        var outboundConnections = Connections.Where(connection => connection.Source.Activity == completedActivity && outcomeNames.Contains(connection.Source.Port)).ToList();
        var children = outboundConnections.Select(x => x.Target.Activity).ToList();
        var scope = flowchartContext.GetProperty(ScopeProperty, () => new FlowScope());

        scope.RegisterActivityExecution(completedActivity);

        // If the complete activity is a terminal node, complete the flowchart immediately.
        if (completedActivity is ITerminalNode)
        {
            await flowchartContext.CompleteActivityAsync();
        }
        else if (scheduleChildren)
        {
            if (children.Any())
            {
                // Schedule each child, but only if all of its left inbound activities have already executed.
                foreach (var activity in children)
                {
                    var existingActivity = scope.ContainsActivity(activity);
                    scope.AddActivity(activity);

                    var inboundActivities = Connections.LeftInboundActivities(activity).ToList();

                    // If the completed activity is not part of the left inbound path, always allow its children to be scheduled.
                    if (!inboundActivities.Contains(completedActivity))
                    {
                        await flowchartContext.ScheduleActivityAsync(activity, OnChildCompletedAsync);
                        continue;
                    }

                    // If the activity is anything but a join activity, only schedule it if all of its left-inbound activities have executed, effectively implementing a "wait all" join. 
                    if (activity is not IJoinNode)
                    {
                        var executionCount = scope.GetExecutionCount(activity);
                        var haveInboundActivitiesExecuted = inboundActivities.All(x => scope.GetExecutionCount(x) > executionCount);

                        if (haveInboundActivitiesExecuted)
                            await flowchartContext.ScheduleActivityAsync(activity, OnChildCompletedAsync);
                    }
                    else
                    {
                        // Select an existing activity execution context for this activity, if any.
                        var joinContext = flowchartContext.WorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x =>
                            x.ParentActivityExecutionContext == flowchartContext && x.Activity == activity);
                        var scheduleWorkOptions = new ScheduleWorkOptions
                        {
                            CompletionCallback = OnChildCompletedAsync,
                            ExistingActivityExecutionContext = joinContext,
                            PreventDuplicateScheduling = true
                        };

                        if (joinContext != null)
                            logger.LogDebug("Next activity {ChildActivityId} is a join activity. Attaching to existing join context {JoinContext}", activity.Id, joinContext.Id);
                        else if (!existingActivity)
                            logger.LogDebug("Next activity {ChildActivityId} is a join activity. Creating new join context", activity.Id);
                        else
                        {
                            logger.LogDebug("Next activity {ChildActivityId} is a join activity. Join context was not found, but activity is already being created", activity.Id);
                            continue;
                        }

                        await flowchartContext.ScheduleActivityAsync(activity, scheduleWorkOptions);
                    }
                }
            }

            if (!children.Any())
            {
                await CompleteIfNoPendingWorkAsync(flowchartContext);
            }
        }

        flowchartContext.SetProperty(ScopeProperty, scope);
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
            await flowchartContext.ScheduleActivityAsync(activityExecutionContext.Activity, new ScheduleWorkOptions
            {
                ExistingActivityExecutionContext = activityExecutionContext,
                CompletionCallback = OnChildCompletedAsync,
                Input = signal.Input
            });
        }
        else
        {
            await flowchartContext.ScheduleActivityAsync(activity, new ScheduleWorkOptions
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