using System.Text.Json.Serialization;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;
using IJavaScriptEvaluator = Elsa.JavaScript.Contracts.IJavaScriptEvaluator;

namespace Elsa.JavaScript.Activities;

/// <summary>
/// Executes JavaScript code.
/// </summary>
[Activity("Elsa", "Scripting", "Executes JavaScript code", DisplayName = "Run JavaScript")]
[PublicAPI]
public class RunJavaScript : CodeActivity<object?>
{
    /// <inheritdoc />
    [JsonConstructor]
    public RunJavaScript()
    {
    }

    /// <inheritdoc />
    public RunJavaScript(string script)
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
        if(result is not null)
            context.Set(Result, result);
    }
}