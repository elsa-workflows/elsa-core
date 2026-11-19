using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.RunAsynchronousActivityOutput.Activities;

/// <summary>
/// A task with a dictionary of outputs that sets each entry by its key.
/// </summary>
[Activity("Elsa.Test", Kind = ActivityKind.Task)]
internal class DynamicOutputsActivity : CodeActivity
{
    public Dictionary<string, Output<object>>? Outputs { get; set; }

    protected override void Execute(ActivityExecutionContext context)
    {
        var values = new Dictionary<string, object>
        {
            ["name"] = "Frank",
            ["age"] = 42d
        };

        if (Outputs == null)
        {
            return;
        }

        foreach (var key in Outputs.Keys)
        {
            context.Set(Outputs, key, values[key]);
        }
    }
}
