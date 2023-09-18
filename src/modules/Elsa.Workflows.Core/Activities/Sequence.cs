using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Signals;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Execute a set of activities in sequence.
/// </summary>
[Category("Workflows")]
[Activity("Elsa", "Workflows", "Execute a set of activities in sequence.")]
[PublicAPI]
[Browsable(false)]
public class Sequence : Container
{
    private const string CurrentIndexProperty = "CurrentIndex";

    /// <inheritdoc />
    public Sequence([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        OnSignalReceived<BreakSignal>(OnBreakSignalReceived);
    }

    /// <inheritdoc />
    protected override async ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        await HandleItemAsync(context);
    }

    private async ValueTask HandleItemAsync(ActivityExecutionContext context)
    {
        var currentIndex = context.GetProperty<int>(CurrentIndexProperty);
        var childActivities = Activities.ToList();

        if (currentIndex >= childActivities.Count)
        {
            await context.CompleteActivityAsync();
            return;
        }

        var nextActivity = childActivities.ElementAt(currentIndex);
        await context.ScheduleActivityAsync(nextActivity, OnChildCompleted);
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

        await HandleItemAsync(targetContext);
    }

    private void OnBreakSignalReceived(BreakSignal signal, SignalContext signalContext)
    {
        signalContext.ReceiverActivityExecutionContext.SetIsBreaking();
    }
}