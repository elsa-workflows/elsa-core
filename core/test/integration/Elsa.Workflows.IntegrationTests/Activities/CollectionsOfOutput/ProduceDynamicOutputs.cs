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
            return;

        foreach (var (key, output) in Outputs)
        {
            if (!values.TryGetValue(key, out var value))
                continue;

            // Record under the dynamic key so it is resolvable by name via the
            // ActivityOutputRegister (the path used by ActivityOutputExpressionHandler: "{activity}|{name}").
            context.Set(output, value, key);
        }
    }
}