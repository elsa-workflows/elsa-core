using System.Text;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Elsa.Scripting.Python.Contracts;
using Elsa.Scripting.Python.Models;
using Elsa.Scripting.Python.Notifications;
using Python.Runtime;

namespace Elsa.Scripting.Python.Services;

/// <summary>
/// Evaluates Python expressions using IronPython.
/// </summary>
public class PythonNetPythonEvaluator : IPythonEvaluator
{
    private const string ReturnVarName = "elsa_python_result_variable_name";
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonNetPythonEvaluator"/> class.
    /// </summary>
    public PythonNetPythonEvaluator(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }

    /// <inheritdoc />
    public async Task<object?> EvaluateAsync(string expression, Type returnType, ExpressionExecutionContext context, CancellationToken cancellationToken = default)
    {
        using var gil = Py.GIL();
        using var scope = Py.CreateScope();
        var notification = new EvaluatingPython(scope, context);
        
        scope.Import("System");

        // Add globals.
        scope.Set("execution_context", new ExecutionContextProxy(context));
        scope.Set("input", new InputProxy(context));
        scope.Set("output", new OutputProxy(context));
        scope.Set("outcome", new OutcomeProxy(context));

        await _notificationSender.SendAsync(notification, cancellationToken);
        var wrappedScript = WrapInExecuteScriptFunction(expression);
        scope.Exec(wrappedScript);
        var result = scope.Get<object>(ReturnVarName);
        return result.ConvertTo(returnType);
    }

    /// <summary>
    /// Wraps the user script in a function called execute_script() and returns the result of that function.
    /// </summary>
    private static string WrapInExecuteScriptFunction(string userScript, int indentationLevel = 1)
    {
        var lines = userScript.Split('\n');
        var wrappedScript = new StringBuilder();
        var indentation = new string(' ', 4 * indentationLevel);
        wrappedScript.AppendLine("def execute_script():");

        foreach (var line in lines.Take(lines.Length - 1))
            wrappedScript.AppendLine(indentation + line);

        var lastLine = lines.LastOrDefault() ?? "";

        if (!lastLine.StartsWith("return"))
            lastLine = $"return {lastLine}";

        wrappedScript.AppendLine(indentation + lastLine);

        wrappedScript.AppendLine($"{ReturnVarName} = execute_script()");
        return wrappedScript.ToString();
    }
}