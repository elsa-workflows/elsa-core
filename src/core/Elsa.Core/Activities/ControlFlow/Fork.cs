using System.Collections.Immutable;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Activities.ControlFlow;

public class Fork : Activity
{
    [Input] public Input<JoinMode> JoinMode { get; set; } = new(ControlFlow.JoinMode.WaitAny);
    [Outbound] public ICollection<IActivity> Branches { get; set; } = new List<IActivity>();

    protected override void Execute(ActivityExecutionContext context) => context.ScheduleActivities(Branches.Reverse(), CompleteChildAsync);

    private ValueTask CompleteChildAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        OnChildCompleted(context, childContext);
        return ValueTask.CompletedTask;
    }

    private void OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var completedChildActivityId = childContext.Activity.Id;

        // Append activity to set of completed activities.
        var completedActivityIds = context.UpdateProperty<HashSet<string>>("Completed", set =>
        {
            set ??= new HashSet<string>();
            set.Add(completedChildActivityId);
            return set;
        });

        var allChildActivityIds = Branches.Select(x => x.Id).ToImmutableHashSet();
        var joinMode = context.Get(JoinMode);

        switch (joinMode)
        {
            case ControlFlow.JoinMode.WaitAny:
            {
                // Remove any and all bookmarks from other branches.
                RemoveBookmarks(context);
            }
                break;
            case ControlFlow.JoinMode.WaitAll:
            {
                var allSet = allChildActivityIds.All(x => completedActivityIds.Contains(x));

                if (!allSet)
                    context.PreventContinuation();
            }
                break;
        }
    }

    private void RemoveBookmarks(ActivityExecutionContext context)
    {
        // Find all descendants for each branch and remove any associated bookmarks.
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var forkNode = context.ActivityNode;
        var branchNodes = forkNode.Children;
        var branchDescendantActivityIds = branchNodes.SelectMany(x => x.Flatten()).Select(x => x.Activity.Id).ToHashSet();
        var bookmarksToRemove = workflowExecutionContext.Bookmarks.Where(x => branchDescendantActivityIds.Contains(x.ActivityId)).ToList();

        workflowExecutionContext.UnregisterBookmarks(bookmarksToRemove);
    }
}