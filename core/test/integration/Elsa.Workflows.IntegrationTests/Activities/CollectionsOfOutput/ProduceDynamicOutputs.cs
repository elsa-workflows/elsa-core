using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionsOfOutput;

public class ProduceDynamicOutputs : CodeActivity
{
    public ProduceDynamicOutputs([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line) { }

    public ProduceDynamicOutputs(Dictionary<string, Output<object>> outputs, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => Outputs = outputs;

    public Dictionary<string, Output<object>>? Outputs { get; set; } = default;

    protected override void Execute(ActivityExecutionContext context)
    {
        var values = new Dictionary<string, object>
        {
            ["name"] = "Frank",
            ["isAdmin"] = false,
            ["age"] = 42d
        };

        if (Outputs == null)
        {
            return;
        }

        foreach (var key in Outputs.Keys)
        {
            if (!values.TryGetValue(key, out var value))
            {
                continue;
            }

            // Records the entry in the ActivityOutputRegister under "Outputs:{key}", the name used to read it
            // (for example by ActivityOutputExpressionHandler: "{activity}|Outputs:{key}").
            context.Set(Outputs, key, value);
        }
    }
}