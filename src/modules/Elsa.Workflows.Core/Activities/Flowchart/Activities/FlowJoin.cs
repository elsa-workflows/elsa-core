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
/// </summary>
[Activity("Elsa", "Branching", "Merge multiple branches into a single branch of execution.", DisplayName = "Join")]
[PublicAPI]
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
        var mode = context.Get(Mode);

        switch (mode)
        {
            case FlowJoinMode.WaitAll:
            {
                if (Flowchart.CanWaitAllProceed(context))
                {
                    Flowchart.CancelAncestorActivatesAsync(context);
                    await context.CompleteActivityAsync();
                }

                break;
            }
            case FlowJoinMode.WaitAny:
            {
                Flowchart.CancelAncestorActivatesAsync(context);
                await context.CompleteActivityAsync();
                break;
            }
        }
    }
}