using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.RunAsynchronousActivityOutput.Activities;

/// <summary>
/// A task with a dictionary of outputs that is not serializable.
/// </summary>
[Activity("Elsa.Test", Kind = ActivityKind.Task)]
internal class NonSerializableDynamicOutputsActivity : CodeActivity
{
    [Output(IsSerializable = false)]
    public Dictionary<string, Output<object>>? Outputs { get; set; }

    protected override void Execute(ActivityExecutionContext context)
    {
        if (Outputs == null)
        {
            return;
        }

        foreach (var key in Outputs.Keys)
        {
            context.Set(Outputs, key, "value");
        }
    }
}
