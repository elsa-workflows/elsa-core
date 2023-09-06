using System.Runtime.CompilerServices;
using Elsa.JavaScript.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.JavaScript.Activities;

/// <summary>
/// Executes JavaScript code.
/// </summary>
[Activity("Elsa", "Scripting", "Executes JavaScript code", DisplayName = "Run JavaScript")]
public class RunJavaScript : CodeActivity<object?>
{
    /// <inheritdoc />
    public RunJavaScript([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public RunJavaScript(string script, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Script = new Input<string>(script);
    }

    /// <summary>
    /// The script to run.
    /// </summary>
    [Input(
        Description = "The script to run.",
        UIHint = InputUIHints.CodeEditor,
        OptionsProvider = typeof(RunJavaScriptOptionsProvider)
    )]
    public Input<string> Script { get; set; } = new("");

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var script = context.Get(Script);

        // If no script was specified, there's nothing to do.
        if (string.IsNullOrWhiteSpace(script))
            return;

        // Get a JavaScript evaluator.
        var javaScriptEvaluator = context.GetRequiredService<IJavaScriptEvaluator>();

        // Run the script.
        var result = await javaScriptEvaluator.EvaluateAsync(script, typeof(object), context.ExpressionExecutionContext, cancellationToken: context.CancellationToken);

        // Set the result as output, if any.
        if (result is not null)
            context.Set(Result, result);
    }
}