using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Signals;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Branch execution into multiple branches.
/// </summary>
[Activity("Elsa", "Control Flow", "Branch execution into multiple branches.")]
[PublicAPI]
[Browsable(false)]
public class Fork : Activity
{
    /// <inheritdoc />
    public Fork([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        // Handle break signals directly instead of using the BreakBehavior. The behavior stops propagation of the signal, which is not what we want.
        OnSignalReceived<BreakSignal>(OnBreakSignalReceived);
    }

    /// <summary>
    /// Controls when this activity yields control back to its parent activity.
    /// </summary>
    [Input(Description = "Controls when this activity yields control back to its parent activity.")]
    public ForkJoinMode JoinMode { get; set; } = ForkJoinMode.WaitAll;

    /// <summary>
    /// The branches to schedule.
    /// </summary>
    [Port]
    public ICollection<IActivity> Branches { get; set; } = new List<IActivity>();

    /// <inheritdoc />
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => context.ScheduleActivities(Branches, CompleteChildAsync);

    private async ValueTask CompleteChildAsync(ActivityCompletedContext context)
    {
        var targetContext = context.TargetContext;
        var isBreaking = targetContext.GetIsBreaking();

        if (isBreaking)
        {
            // Remove any and all bookmarks from other branches.
            RemoveBookmarks(targetContext);

            // Signal activity completion.
            await CompleteAsync(targetContext);

            // Exit.
            return;
        }

        var childContext = context.ChildContext;
        var completedChildActivityId = childContext.Activity.Id;

        // Append activity to set of completed activities.
        var completedActivityIds = targetContext.UpdateProperty<HashSet<string>>("Completed", set =>
        {
            set ??= new HashSet<string>();
            set.Add(completedChildActivityId);
            return set;
        });

        var allChildActivityIds = Branches.Select(x => x.Id).ToImmutableHashSet();
        var joinMode = JoinMode;

        switch (joinMode)
        {
            case ForkJoinMode.WaitAny:
            {
                // Remove any and all bookmarks from other branches.
                RemoveBookmarks(targetContext);

                // Signal activity completion.
                await CompleteAsync(targetContext);
            }
                break;
            case ForkJoinMode.WaitAll:
            {
                var allSet = allChildActivityIds.All(x => completedActivityIds.Contains(x));

                if (allSet)
                    // Signal activity completion.
                    await CompleteAsync(targetContext);
            }
                break;
        }
    }

    private void RemoveBookmarks(ActivityExecutionContext context)
    {
        // Find all descendants for each branch and remove them as well as any associated bookmarks.
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var forkNode = context.ActivityNode;
        var branchNodes = forkNode.Children;
        var branchDescendantActivityIds = branchNodes.SelectMany(x => x.Flatten()).Select(x => x.Activity.Id).ToHashSet();

        workflowExecutionContext.Bookmarks.RemoveWhere(x => branchDescendantActivityIds.Contains(x.ActivityId));
    }

    private void OnBreakSignalReceived(BreakSignal signal, SignalContext signalContext)
    {
        signalContext.ReceiverActivityExecutionContext.SetIsBreaking();
    }
}