using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Microsoft.CodeAnalysis.Scripting;

namespace Elsa.CSharp.Notifications;

/// <summary>
/// This notification is published every time a C# expression is about to be evaluated.
/// It gives subscribers a chance to configure the <see cref="ScriptOptions"/> with additional functions and variables.
/// </summary>
public record EvaluatingCSharp(ExpressionEvaluatorOptions Options, Script Script, ScriptOptions ScriptOptions, ExpressionExecutionContext Context) : INotification
{
    /// <summary>
    /// Configures the <see cref="ScriptOptions"/> with additional functions and variables.
    /// </summary>
    public EvaluatingCSharp ConfigureScriptOptions(Func<ScriptOptions, ScriptOptions> scriptOptions)
    {
        ScriptOptions = scriptOptions(ScriptOptions);
        return this;
    }

    /// <summary>
    /// Appends the specified script to the current script.
    /// </summary>
    public EvaluatingCSharp AppendScript(string script)
    {
        Script = Script.ContinueWith(script, ScriptOptions);
        return this;
    }

    /// <summary>
    /// The <see cref="ScriptOptions"/> that will be used to evaluate the expression.
    /// </summary>
    public ScriptOptions ScriptOptions { get; set; } = ScriptOptions;

    /// <summary>
    /// The <see cref="ScriptState"/> that will be used to evaluate the expression.
    /// </summary>
    public Script Script { get; set; } = Script;
}