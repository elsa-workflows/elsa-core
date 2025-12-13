using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

/// <summary>
/// A flowchart consists of a collection of activities and connections between them.
/// </summary>
[Activity("Elsa", "Flow", "A flowchart is a collection of activities and connections between them.")]
[Browsable(false)]
public partial class Flowchart : Container
{
    /// <summary>
    /// Set this to <c>false</c> from your program file in case you wish to use the old counter based model.
    /// </summary>
    public static bool UseTokenFlow = true;

    /// <inheritdoc />
    public Flowchart([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        OnSignalReceived<ScheduleActivityOutcomes>(OnScheduleOutcomesAsync);
        OnSignalReceived<ScheduleChildActivity>(OnScheduleChildActivityAsync);
        OnSignalReceived<CancelSignal>(OnActivityCanceledAsync);
    }

    /// <summary>
    /// The activity to execute when the flowchart starts.
    /// </summary>
    [Port][Browsable(false)] public IActivity? Start { get; set; }

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

        await context.ScheduleActivityAsync(startActivity, OnChildCompletedAsync);
    }

    private async ValueTask OnScheduleChildActivityAsync(ScheduleChildActivity signal, SignalContext context)
    {
        var flowchartContext = context.ReceiverActivityExecutionContext;
        var activity = signal.Activity;
        var activityExecutionContext = signal.ActivityExecutionContext;

        if (activityExecutionContext != null)
        {
            await flowchartContext.ScheduleActivityAsync(activityExecutionContext.Activity, new()
            {
                ExistingActivityExecutionContext = activityExecutionContext,
                CompletionCallback = OnChildCompletedAsync,
                Input = signal.Input
            });
        }
        else
        {
            await flowchartContext.ScheduleActivityAsync(activity, new()
            {
                CompletionCallback = OnChildCompletedAsync,
                Input = signal.Input
            });
        }
    }

    private ValueTask OnChildCompletedAsync(ActivityCompletedContext context)
    {
        return UseTokenFlow
            ? OnChildCompletedTokenBasedLogicAsync(context)
            : OnChildCompletedCounterBasedLogicAsync(context);
    }

    private ValueTask OnActivityCanceledAsync(CancelSignal signal, SignalContext context)
    {
        return UseTokenFlow
            ? OnTokenFlowActivityCanceledAsync(signal, context)
            : OnCounterFlowActivityCanceledAsync(signal, context);
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
}