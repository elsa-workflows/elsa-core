using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;
using IJavaScriptEvaluator = Elsa.JavaScript.Services.IJavaScriptEvaluator;

namespace Elsa.JavaScript.Activities;

[Activity("Elsa", "Scripting", "Executes JavaScript code")]
public class RunJavaScript : Activity<object?>
{
    [JsonConstructor]
    public RunJavaScript()
    {
    }

    public RunJavaScript(string script)
    {
        Script = new Input<string>(script);
    }

    [Input(UIHint = InputUIHints.CodeEditor, OptionsProvider = typeof(RunJavaScriptOptionsProvider))]
    public Input<string> Script { get; set; } = new("");

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
        context.Set(Result, result);
    }
}