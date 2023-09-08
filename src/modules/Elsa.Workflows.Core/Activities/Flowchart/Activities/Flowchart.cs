using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Contracts;
using Elsa.Workflows.Core.Activities.Flowchart.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

/// <summary>
/// A flowchart consists of a collection of activities and connections between them.
/// </summary>
[Activity("Elsa", "Flow", "A flowchart is a collection of activities and connections between them.")]
[Browsable(false)]
public class Flowchart : Container
{
    internal const string ScopeProperty = "Scope";

    /// <inheritdoc />
    public Flowchart([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
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
        await context.ScheduleActivityAsync(startActivity, OnChildCompletedAsync);
    }

    private async ValueTask OnChildCompletedAsync(ActivityCompletedContext context)
    {
        var targetContext = context.TargetContext;
        var childContext = context.ChildContext;
        var completedActivity = childContext.Activity;
        var result = context.Result;
        var alreadyCompleted = result is AlreadyCompleted;

        // If specific outcomes were provided by the completed activity, use them to find the connection to the next activity.
        Func<Connection, bool> outboundConnectionsQuery = result is Outcomes outcomes
            ? connection => connection.Source.Activity == completedActivity && outcomes.Names.Contains(connection.Source.Port)
            : connection => connection.Source.Activity == completedActivity;

        // Only query the outbound connections if the completed activity wasn't already completed.
        var outboundConnections = alreadyCompleted ? new List<Connection>() : Connections.Where(outboundConnectionsQuery).ToList();
        var children = outboundConnections.Select(x => x.Target.Activity).ToList();
        var scope = targetContext.GetProperty(ScopeProperty, () => new FlowScope());

        scope.RegisterActivityExecution(completedActivity);

        // If the completed activity is an End or Break activity, complete the flowchart immediately.
        if (completedActivity is End or Break)
        {
            await targetContext.CompleteActivityAsync();
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
                        await targetContext.ScheduleActivityAsync(activity, OnChildCompletedAsync);
                        continue;
                    }

                    // If the activity is anything but a join activity, only schedule it if all of its left-inbound activities have executed, effectively implementing a "wait all" join. 
                    if (activity is not IJoinNode)
                    {
                        var executionCount = scope.GetExecutionCount(activity);
                        var haveInboundActivitiesExecuted = inboundActivities.All(x => scope.GetExecutionCount(x) > executionCount);

                        if (haveInboundActivitiesExecuted)
                            await targetContext.ScheduleActivityAsync(activity, OnChildCompletedAsync);
                    }
                    else
                    {
                        await targetContext.ScheduleActivityAsync(activity, OnChildCompletedAsync);
                    }
                }
            }

            if (!children.Any())
            {
                // If there is no pending work, complete the flowchart activity.
                var hasPendingWork = HasPendingWork(targetContext);

                if (!hasPendingWork)
                    await targetContext.CompleteActivityAsync();
            }
        }

        targetContext.SetProperty(ScopeProperty, scope);
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

            if (ownerInstanceId == null)
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