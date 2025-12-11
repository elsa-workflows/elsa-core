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
        return GetEffectiveExecutionMode(context.TargetContext)
            ? OnChildCompletedTokenBasedLogicAsync(context)
            : OnChildCompletedCounterBasedLogicAsync(context);
    }

    private ValueTask OnActivityCanceledAsync(CancelSignal signal, SignalContext context)
    {
        return GetEffectiveExecutionMode(context.ReceiverActivityExecutionContext)
            ? OnTokenFlowActivityCanceledAsync(signal, context)
            : OnCounterFlowActivityCanceledAsync(signal, context);
    }

    /// <summary>
    /// Gets the effective execution mode for this flowchart execution.
    /// Returns true for token-based mode, false for counter-based mode.
    /// Priority: WorkflowExecutionContext.Properties > FlowchartOptions (DI) > Static UseTokenFlow flag
    /// </summary>
    private bool GetEffectiveExecutionMode(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;

        if (workflowExecutionContext.Properties.TryGetValue(ExecutionModePropertyKey, out var modeValue))
        {
            var mode = ParseExecutionMode(modeValue);

            if (mode != FlowchartExecutionMode.Default)
                return ConvertModeToBoolean(mode);
        }

        var defaultMode = GetDefaultModeFromOptionsAsEnum(context);
        return ConvertModeToBoolean(defaultMode);
    }

    private FlowchartExecutionMode ParseExecutionMode(object modeValue)
    {
        return modeValue switch
        {
            FlowchartExecutionMode executionMode => executionMode,
            string str when Enum.TryParse<FlowchartExecutionMode>(str, true, out var parsed) => parsed,
            int intValue when Enum.IsDefined(typeof(FlowchartExecutionMode), intValue) => (FlowchartExecutionMode)intValue,
            _ => FlowchartExecutionMode.Default
        };
    }

    private bool ConvertModeToBoolean(FlowchartExecutionMode mode)
    {
        return mode switch
        {
            FlowchartExecutionMode.TokenBased => true,
            FlowchartExecutionMode.CounterBased => false,
            _ => UseTokenFlow
        };
    }

    private FlowchartExecutionMode GetDefaultModeFromOptionsAsEnum(ActivityExecutionContext context)
    {
        var options = context.WorkflowExecutionContext.ServiceProvider.GetService<IOptions<FlowchartOptions>>();
        return options?.Value.DefaultExecutionMode ?? FlowchartExecutionMode.Default;
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