using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Contracts;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

/// <summary>
/// Merge multiple branches into a single branch of execution.
/// Note that this activity is no longer necessary for either AND or OR merges, because all activities inherit the Join Kind property.
/// Use this activity if an explicit join step is desired.
/// </summary>
[Activity("Elsa", "Branching", "Explicitly merge multiple branches into a single branch of execution.", DisplayName = "Join")]
[UsedImplicitly]
[Obsolete("Each activity now supports the MergeMode property, making the use of this activity obsolete.", false)]
public class FlowJoin : Activity, IJoinNode
{
    /// <inheritdoc />
    public FlowJoin([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The join mode determines whether this activity should continue as soon as one inbound path comes in (Wait Any), or once all inbound paths have executed (Wait All).
    /// </summary>
    [Input(
        Description = "The join mode determines whether this activity should continue as soon as one inbound path comes in (Wait Any), or once all inbound paths have executed (Wait All).",
        DefaultValue = FlowJoinMode.WaitAny,
        UIHint = InputUIHints.DropDown
    )]
    public Input<FlowJoinMode> Mode { get; set; } = new(FlowJoinMode.WaitAny);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if(!Flowchart.UseTokenFlow)
            await context.ParentActivityExecutionContext.CancelInboundAncestorsAsync(this);
        
        await context.CompleteActivityAsync();
    }

    protected override bool CanExecute(ActivityExecutionContext context)
    {
        if(Flowchart.UseTokenFlow)
            return true;
        
        return context.Get(Mode) switch
        {
            FlowJoinMode.WaitAny => true,
            FlowJoinMode.WaitAll => Flowchart.CanWaitAllProceed(context),
            _ => true
        };
    }
}