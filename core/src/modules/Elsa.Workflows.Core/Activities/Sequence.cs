using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Options;
using Elsa.Workflows.Signals;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Execute a set of activities in sequence.
/// </summary>
/// <remarks>
/// Rescheduled direct children retain completion callback ownership by this sequence.
/// </remarks>
[Category("Workflows")]
[Activity("Elsa", "Workflows", "Execute a set of activities in sequence.")]
[PublicAPI]
[Browsable(false)]
public class Sequence : Container
{
    private const string CurrentIndexProperty = "CurrentIndex";

    /// <inheritdoc />
    public Sequence([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        OnSignalReceived<BreakSignal>(OnBreakSignalReceived);
        OnSignalReceived<ScheduleChildActivity>(OnScheduleChildActivityAsync);
    }

    /// <inheritdoc />
    protected override async ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        await HandleItemAsync(context);
    }

    private async ValueTask HandleItemAsync(ActivityExecutionContext context, ActivityExecutionContext? completedChildContext = null)
    {
        var currentIndex = context.GetProperty<int>(CurrentIndexProperty);
        var childActivities = Activities.ToList();

        if (currentIndex >= childActivities.Count)
        {
            await context.CompleteActivityAsync();
            return;
        }

        var nextActivity = childActivities.ElementAt(currentIndex);
        var options = new ScheduleWorkOptions
        {
            CompletionCallback = OnChildCompleted,
            SchedulingActivityExecutionId = completedChildContext?.Id
        };
        await context.ScheduleActivityAsync(nextActivity, options);
        context.UpdateProperty<int>(CurrentIndexProperty, x => x + 1);
    }

    private async ValueTask OnChildCompleted(ActivityCompletedContext context)
    {
        var targetContext = context.TargetContext;
        var childContext = context.ChildContext;
        var isBreaking = targetContext.GetIsBreaking();
        var completedActivity = childContext.Activity;

        // If the complete activity is a terminal node, complete the sequence immediately.
        if (isBreaking || completedActivity is ITerminalNode)
        {
            await targetContext.CompleteActivityAsync();
            return;
        }

        await HandleItemAsync(targetContext, childContext);
    }

    private async ValueTask OnScheduleChildActivityAsync(ScheduleChildActivity signal, SignalContext context)
    {
        var sequenceContext = context.ReceiverActivityExecutionContext;
        var childActivity = signal.ActivityExecutionContext?.Activity ?? signal.Activity;

        if (childActivity == null || !Activities.Contains(childActivity))
        {
            return;
        }

        context.StopPropagation();
        await sequenceContext.ScheduleActivityAsync(childActivity, new ScheduleWorkOptions
        {
            ExistingActivityExecutionContext = signal.ActivityExecutionContext,
            CompletionCallback = OnChildCompleted,
            Input = signal.Input
        });
    }

    private void OnBreakSignalReceived(BreakSignal signal, SignalContext signalContext)
    {
        signalContext.ReceiverActivityExecutionContext.SetIsBreaking();
    }
}
