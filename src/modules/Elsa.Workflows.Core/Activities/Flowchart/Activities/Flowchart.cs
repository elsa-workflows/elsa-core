using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        var startActivity = GetStartActivity(context);

        if (startActivity == null)
        {
            // Nothing else to execute.
            await context.CompleteActivityAsync();
            return;
        }

        // Schedule the start activity.
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

        // If the completed activity is an End activity, complete the flowchart.
        if (completedActivity is End)
        {
            await flowchartActivityExecutionContext.CompleteActivityAsync();
        }
        else
        {
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
                // If there is no pending work, complete the flowchart activity.
                var hasPendingWork = HasPendingWork(flowchartActivityExecutionContext);

                if (!hasPendingWork)
                    await flowchartActivityExecutionContext.CompleteActivityAsync();
            }
        }

        flowchartActivityExecutionContext.SetProperty(ScopeProperty, scope);
        context.StopPropagation();
    }

    /// <summary>
    /// Checks if there is any pending work for the flowchart.
    /// </summary>
    private bool HasPendingWork(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var activityIds = Activities.Select(x => x.Id).ToList();
        var descendantContexts = GetDescendents(context).Where(x => x.ParentActivityExecutionContext == context).ToList();
        var activityExecutionContexts = descendantContexts.Where(x => activityIds.Contains(x.Activity.Id)).ToList();
        
        var hasPendingWork = workflowExecutionContext.Scheduler.List().Any(workItem =>
        {
            var ownerInstanceId = workItem.OwnerActivityInstanceId;
            
            if(ownerInstanceId == null)
                return false;

            var ownerContext = context.WorkflowExecutionContext.ActiveActivityExecutionContexts.First(x => x.Id == ownerInstanceId);
            var ancestors = GetAncestors(ownerContext).ToList();

            return ancestors.Any(x => x == context);

        });
        
        var hasRunningActivityInstances = activityExecutionContexts.Any(x => x.Status == ActivityStatus.Running);

        return hasRunningActivityInstances || hasPendingWork;
    }

    private static IEnumerable<ActivityExecutionContext> GetDescendents(ActivityExecutionContext activityExecutionContext)
    {
        var children = activityExecutionContext.WorkflowExecutionContext.ActiveActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == activityExecutionContext).ToList();
        
        foreach (var child in children)
        {
            yield return child;

            foreach (var descendent in GetDescendents(child))
                yield return descendent;
        }
    }

    private static IEnumerable<ActivityExecutionContext> GetAncestors(ActivityExecutionContext activityExecutionContext)
    {
        var current = activityExecutionContext;
        
        while (current != null)
        {
            yield return current;
            current = current.ParentActivityExecutionContext;
        }
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

        // If there is a single activity that has no inbound connections, use that.
        var root = GetRootActivity();

        if (root != null)
            return root;

        // If no start activity found, return the first activity.
        return Activities.FirstOrDefault();
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
}