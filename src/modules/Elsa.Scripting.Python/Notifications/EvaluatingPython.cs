using System.Text;
using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Python.Runtime;

namespace Elsa.Scripting.Python.Notifications;

/// <summary>
/// This notification is published every time a Python expression is about to be evaluated, giving subscribers a chance to modify the Python engine.
/// </summary>
public record EvaluatingPython(PyModule Scope, ExpressionExecutionContext Context) : INotification
{
    /// <summary>
    /// Appends a script to the Python engine.
    /// </summary>
    /// <param name="builder">A builder that builds the script to append.</param>
    public void AppendScript(Action<StringBuilder> builder)
    {
        var sb = new StringBuilder();
        builder(sb);
        AppendScript(sb.ToString());
    }

    /// <summary>
    /// Appends a script to the Python engine.
    /// </summary>
    /// <param name="script">The script to append.</param>
    public void AppendScript(string script)
    {
        Scope.Exec(script);
    }
}