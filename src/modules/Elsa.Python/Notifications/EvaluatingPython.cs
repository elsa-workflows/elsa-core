using System.Text;
using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Microsoft.Scripting.Hosting;

namespace Elsa.Python.Notifications;

/// <summary>
/// This notification is published every time a Python expression is about to be evaluated, giving subscribers a chance to modify the Python engine.
/// </summary>
public record EvaluatingPython(ScriptEngine Engine, ScriptScope ScriptScope, ExpressionExecutionContext Context) : INotification
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
        var engine = Engine;
        engine.Execute(script, ScriptScope);
    }
}