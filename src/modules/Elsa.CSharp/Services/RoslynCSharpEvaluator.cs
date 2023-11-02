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
public class RoslynCSharpEvaluator : ICSharpEvaluator
{
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynCSharpEvaluator"/> class.
    /// </summary>
    public RoslynCSharpEvaluator(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }

    /// <inheritdoc />
    public async Task<object?> EvaluateAsync(string expression, Type returnType, ExpressionExecutionContext context, CancellationToken cancellationToken = default)
    {
        var scriptOptions = ScriptOptions.Default
            .AddReferences(typeof(Globals).Assembly, typeof(Enumerable).Assembly)
            .AddImports(typeof(Globals).Namespace, typeof(Enumerable).Namespace);

        var globals = new Globals(new ExecutionContextProxy(context));
        var script = CSharpScript.Create("", scriptOptions, typeof(Globals));
        var notification = new EvaluatingCSharp(script, scriptOptions, context);
        await _notificationSender.SendAsync(notification, cancellationToken);
        scriptOptions = notification.ScriptOptions;
        script = notification.Script.ContinueWith(expression, scriptOptions);
        var scriptState = await script.RunAsync(globals, cancellationToken: cancellationToken);
        return scriptState.ReturnValue;
    }
}