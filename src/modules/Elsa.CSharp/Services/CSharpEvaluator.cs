using Elsa.CSharp.Contracts;
using Elsa.CSharp.Models;
using Elsa.CSharp.Notifications;
using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Elsa.CSharp.Services;

/// <summary>
/// A C# expression evaluator using Roslyn.
/// </summary>
public class CSharpEvaluator : ICSharpEvaluator
{
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpEvaluator"/> class.
    /// </summary>
    public CSharpEvaluator(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }

    /// <inheritdoc />
    public async Task<object?> EvaluateAsync(
        string expression,
        Type returnType,
        ExpressionExecutionContext context,
        Func<ScriptOptions, ScriptOptions>? configureScriptOptions = default,
        Func<Script<object>, Script<object>>? configureScript = default,
        CancellationToken cancellationToken = default)
    {
        var scriptOptions = ScriptOptions.Default;

        if (configureScriptOptions != null)
            scriptOptions = configureScriptOptions(scriptOptions);
        
        var globals = new Globals(context);
        var script = CSharpScript.Create("", scriptOptions, typeof(Globals));
        
        if (configureScript != null)
            script = configureScript(script);

        var notification = new EvaluatingCSharp(script, scriptOptions, context);
        await _notificationSender.SendAsync(notification, cancellationToken);
        scriptOptions = notification.ScriptOptions;
        script = notification.Script.ContinueWith(expression, scriptOptions);
        var scriptState = await script.RunAsync(globals, cancellationToken: cancellationToken);
        return scriptState.ReturnValue;
    }
}