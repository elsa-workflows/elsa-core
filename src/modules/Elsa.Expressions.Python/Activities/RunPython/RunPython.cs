using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Expressions.Python.Contracts;
using Elsa.Expressions.Python.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Expressions.Python.Activities;

/// <summary>
/// Executes Python code.
/// </summary>
[Activity("Elsa", "Scripting", "Executes Python code", DisplayName = "Run Python")]
public class RunPython : CodeActivity<object?>
{
    /// <inheritdoc />
    public RunPython([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public RunPython(string script, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Script = new Input<string>(script);
    }
    
    /// <summary>
    /// The script to run.
    /// </summary>
    [Input(
        Description = "The script to run.",
        DefaultSyntax = "Python",
        UIHint = InputUIHints.CodeEditor,
        UIHandler = typeof(RunPythonOptionsProvider)
    )]
    public Input<string> Script { get; set; } = new("");

    /// <summary>
    /// A list of possible outcomes. Use "SetOutcome(string)" to set the outcome. Use "SetOutcomes(params string[])" to set multiple outcomes.
    /// </summary>
    [Input(Description = "A list of possible outcomes.", UIHint = InputUIHints.DynamicOutcomes)]
    public Input<ICollection<string>> PossibleOutcomes { get; set; } = default!;
    
    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var script = context.Get(Script);

        // If no script was specified, there's nothing to do.
        if (string.IsNullOrWhiteSpace(script))
            return;

        // Get a Python evaluator.
        var evaluator = context.GetRequiredService<IPythonEvaluator>();

        // Run the script.
        var result = await evaluator.EvaluateAsync(script, typeof(object), context.ExpressionExecutionContext, context.CancellationToken);

        // Set the result as output, if any.
        if (result is not null)
            context.Set(Result, result);

        // Get the outcome or outcomes set by the script, if any. If not set, use "Done".
        var outcomes = context.ExpressionExecutionContext.TransientProperties.GetValueOrDefault(OutcomeProxy.OutcomePropertiesKey, () => new[] { "Done" })!;

        // Complete the activity with the outcome.
        await context.CompleteActivityWithOutcomesAsync(outcomes);
    }
}