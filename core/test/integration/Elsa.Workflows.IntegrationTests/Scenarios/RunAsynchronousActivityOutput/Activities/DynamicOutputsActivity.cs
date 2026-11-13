using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.RunAsynchronousActivityOutput.Activities;

/// <summary>
/// A task with a dictionary of outputs that sets each output under its key.
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

        foreach (var (key, output) in Outputs ?? [])
        {
            context.Set(output, values[key], key);
        }
    }
}
