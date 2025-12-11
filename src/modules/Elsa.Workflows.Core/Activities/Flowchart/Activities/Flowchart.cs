using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Activities.Flowchart.Options;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Signals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

/// <summary>
/// A flowchart consists of a collection of activities and connections between them.
/// </summary>
[Activity("Elsa", "Flow", "A flowchart is a collection of activities and connections between them.")]
[Browsable(false)]
public partial class Flowchart : Container
{
    /// <summary>
    /// The property key used to store the flowchart execution mode in <see cref="WorkflowExecutionContext.Properties"/>.
    /// </summary>
    public const string ExecutionModePropertyKey = "Flowchart:ExecutionMode";

    /// <summary>
    /// Set this to <c>false</c> from your program file in case you wish to use the old counter based model.
    /// This static field is used as a final fallback when no execution mode is specified via options or workflow execution context properties.
    /// Note: Prefer using <see cref="FlowchartOptions"/> configured via DI for application-wide settings.
    /// </summary>
    // ReSharper disable once GrammarMistakeInComment
    public static bool UseTokenFlow = false; // Default to false to maintain the same behavior with 3.5.2 out of the box.

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
        return ExecuteBasedOnMode(
            context.TargetContext,
            () => OnChildCompletedTokenBasedLogicAsync(context),
            () => OnChildCompletedCounterBasedLogicAsync(context));
    }

    private ValueTask OnActivityCanceledAsync(CancelSignal signal, SignalContext context)
    {
        return ExecuteBasedOnMode(
            context.ReceiverActivityExecutionContext,
            () => OnTokenFlowActivityCanceledAsync(signal, context),
            () => OnCounterFlowActivityCanceledAsync(signal, context));
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

    private static ValueTask ExecuteBasedOnMode(ActivityExecutionContext context, Func<ValueTask> tokenBasedAction, Func<ValueTask> counterBasedAction)
    {
        var mode = GetEffectiveExecutionMode(context);

        return mode switch
        {
            FlowchartExecutionMode.TokenBased => tokenBasedAction(),
            FlowchartExecutionMode.CounterBased or FlowchartExecutionMode.Default or _ => counterBasedAction()
        };
    }

    /// <summary>
    /// Gets the effective execution mode for this flowchart execution.
    /// Priority: WorkflowExecutionContext.Properties > FlowchartOptions (DI) > Static UseTokenFlow flag
    /// </summary>
    private static FlowchartExecutionMode GetEffectiveExecutionMode(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;

        if (!workflowExecutionContext.Properties.TryGetValue(ExecutionModePropertyKey, out var modeValue))
            return GetDefaultModeFromOptions(context);

        var mode = ParseExecutionMode(modeValue);
        return mode != FlowchartExecutionMode.Default ? mode : GetDefaultModeFromOptions(context);
    }

    private static FlowchartExecutionMode ParseExecutionMode(object modeValue)
    {
        return modeValue switch
        {
            FlowchartExecutionMode executionMode => executionMode,
            string str when Enum.TryParse<FlowchartExecutionMode>(str, true, out var parsed) => parsed,
            int intValue when Enum.IsDefined(typeof(FlowchartExecutionMode), intValue) => (FlowchartExecutionMode)intValue,
            _ => FlowchartExecutionMode.Default
        };
    }

    private static FlowchartExecutionMode GetDefaultModeFromOptions(ActivityExecutionContext context)
    {
        var options = context.WorkflowExecutionContext.ServiceProvider.GetService<IOptions<FlowchartOptions>>();
        return options?.Value.DefaultExecutionMode ?? FlowchartExecutionMode.Default;
    }
}