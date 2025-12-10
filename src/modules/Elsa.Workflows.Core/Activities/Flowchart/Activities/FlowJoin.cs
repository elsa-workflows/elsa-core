using System.Runtime.CompilerServices;
using Elsa.Workflows.Activities.Flowchart.Contracts;
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
        Description = "The join mode determines whether this activity should continue as soon as one inbound path comes in (WaitAny), or once all inbound paths have executed (WaitAll). To wait for all activated inbound paths, set the mode to WaitAllActive.",
        DefaultValue = FlowJoinMode.WaitAny,
        UIHint = InputUIHints.DropDown
    )]
    public Input<FlowJoinMode> Mode { get; set; } = new(FlowJoinMode.WaitAny);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if(!Flowchart.UseTokenFlow)
            if (context.ParentActivityExecutionContext != null)
                await context.ParentActivityExecutionContext.CancelInboundAncestorsAsync(this);

        // The join behavior is handled by Flowchart, so we can simply complete the activity here.
        await context.CompleteActivityAsync();
    }
}