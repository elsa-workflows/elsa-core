using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

/// <summary>
/// A flowchart consists of a collection of activities and connections between them.
/// </summary>
[Activity("Elsa", "Flow", "A flowchart is a collection of activities and connections between them.")]
[Browsable(false)]
public partial class Flowchart : Container
{
    public const bool UseTokenFlow = true;

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
        var startActivity = this.GetStartActivity(context.WorkflowExecutionContext.TriggerActivityId);

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

    private async ValueTask OnChildCompletedAsync(ActivityCompletedContext context)
    {
        if (UseTokenFlow)
            await OnChildCompletedTokenBasedLogicAsync(context);
        else
            await OnChildCompletedCounterBasedLogicAsync(context);
    }
    
    private async ValueTask OnActivityCanceledAsync(CancelSignal signal, SignalContext context)
    {
        if (UseTokenFlow)
            return;
        
        await CompleteIfNoPendingWorkAsync(context.ReceiverActivityExecutionContext);

        var flowchartContext = context.ReceiverActivityExecutionContext;
        var flowchart = (Flowchart)flowchartContext.Activity;
        var flowGraph = flowchartContext.GetFlowGraph();
        var flowScope = flowchart.GetFlowScope(flowchartContext);

        // Propagate canceled connections visited count by scheduling with Outcomes.Empty
        await flowchart.ScheduleOutboundActivitiesAsync(flowGraph, flowScope, flowchartContext, context.SenderActivityExecutionContext.Activity, Outcomes.Empty);
    }
}