using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Elsa.Python.Contracts;
using Elsa.Python.Models;
using Elsa.Python.Notifications;

namespace Elsa.Python.Services;

/// <summary>
/// Evaluates Python expressions using IronPython.
/// </summary>
public class IronPythonEvaluator : IPythonEvaluator
{
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Initializes a new instance of the <see cref="IronPythonEvaluator"/> class.
    /// </summary>
    public IronPythonEvaluator(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }
    
    /// <inheritdoc />
    public async Task<object?> EvaluateAsync(string expression, Type returnType, ExpressionExecutionContext context, CancellationToken cancellationToken = default)
    {
        var engine = IronPython.Hosting.Python.CreateEngine();
        var scope = engine.CreateScope();
        var notification = new EvaluatingPython(engine, scope, context);
        
        // Add imports.
        notification.AppendScript(sb =>
        {
            sb.AppendLine("import System");
            sb.AppendLine("import clr");
        });
        
        // Add globals.
        scope.SetVariable("execution_context", new ExecutionContextProxy(context));
        scope.SetVariable("input", new InputProxy(context));
        scope.SetVariable("output", new OutputProxy(context));
        scope.SetVariable("outcome", new OutcomeProxy(context));
        
        await _notificationSender.SendAsync(notification, cancellationToken);
        var result = (object?)engine.Execute(expression, scope);
        return result.ConvertTo(returnType);
    }
}