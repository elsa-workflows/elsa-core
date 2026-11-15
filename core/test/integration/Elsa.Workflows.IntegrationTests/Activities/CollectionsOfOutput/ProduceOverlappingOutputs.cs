using System.Runtime.CompilerServices;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionsOfOutput;

/// <summary>
/// Has a single output and two dictionary outputs whose entries can use the same key as the single output.
/// </summary>
public class ProduceOverlappingOutputs : CodeActivity
{
    public ProduceOverlappingOutputs([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line) { }

    public Output<string>? Result { get; set; }
    public Dictionary<string, Output<object>> Outputs { get; set; } = new();
    public Dictionary<string, Output<object>> MoreOutputs { get; set; } = new();

    protected override void Execute(ActivityExecutionContext context)
    {
        context.Set(Result, "single");

        foreach (var key in Outputs.Keys)
        {
            context.Set(Outputs, key, $"outputs-{key}");
        }

        foreach (var key in MoreOutputs.Keys)
        {
            context.Set(MoreOutputs, key, $"more-outputs-{key}");
        }
    }
}
