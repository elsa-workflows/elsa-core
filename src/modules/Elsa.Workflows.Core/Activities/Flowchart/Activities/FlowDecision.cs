using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

[FlowNode("True", "False")]
[Activity("Elsa", "Flow", "Evaluate a Boolean condition to determine which path to execute next.")]
public class FlowDecision : ActivityBase
{
    /// <summary>
    /// The condition to evaluate.
    /// </summary>
    [Input(UIHint = "single-line")]
    public Input<bool> Condition { get; set; } = new(new Literal<bool>(false));

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var result = context.Get(Condition);
        var outcome = result ? "True" : "False";

        await context.CompleteActivityAsync(new Outcome(outcome));
    }
}