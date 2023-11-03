using System.Reflection;
using Elsa.CSharp.Contexts;
using Elsa.CSharp.Models;
using Elsa.Expressions.Models;
using Microsoft.CodeAnalysis.Scripting;

namespace Elsa.CSharp.Options;

/// <summary>
/// Options for the C# expression evaluator.
/// </summary>
public class CSharpOptions
{
    /// <summary>
    /// A list of callbacks that is invoked when a C# expression is evaluated. Use this to configure the <see cref="ScriptOptions"/>.
    /// </summary>
    public ICollection<Func<ScriptOptions, ExpressionExecutionContext, ScriptOptions>> ConfigureScriptOptionsCallbacks { get; } = new List<Func<ScriptOptions, ExpressionExecutionContext, ScriptOptions>>();

    /// <summary>
    /// A list of callbacks that is invoked when a C# expression is evaluated. Use this to configure the <see cref="Script"/>.
    /// </summary>
    public ICollection<Func<Script, ExpressionExecutionContext, Script>> ConfigureScriptCallbacks { get; } = new List<Func<Script, ExpressionExecutionContext, Script>>();

    /// <summary>
    /// A list of assemblies that will be referenced when evaluating a C# expression.
    /// </summary>
    public ISet<Assembly> Assemblies { get; } = new HashSet<Assembly>(new[]
    {
        typeof(Globals).Assembly, // Elsa.CSharp
        typeof(Enumerable).Assembly // System.Linq
    });

    /// <summary>
    /// A list of namespaces that will be imported when evaluating a C# expression.
    /// </summary>
    public ISet<string> Namespaces { get; } = new HashSet<string>(new[]
    {
        typeof(Globals).Namespace!, // Elsa.CSharp
        typeof(Enumerable).Namespace! // System.Linq
    });

    /// <summary>
    /// Configures the <see cref="ScriptOptions"/>.
    /// </summary>
    /// <param name="configurator">A callback that is invoked before a C# expression is evaluated. Use this to configure the <see cref="ScriptOptions"/>.</param>
    public CSharpOptions ConfigureScriptOptions(Action<ScriptOptionsConfigurationContext> configurator)
    {
        ConfigureScriptOptionsCallbacks.Add((scriptOptions, context) =>
        {
            configurator(new ScriptOptionsConfigurationContext(scriptOptions, context));
            return scriptOptions;
        });
        return this;
    }

    /// <summary>
    /// Appends a C# script to the current script.
    /// </summary>
    public CSharpOptions AppendScript(string script)
    {
        ConfigureScriptCallbacks.Add((s, _) => s.ContinueWith(script));
        return this;
    }

    /// <summary>
    /// Appends a C# script to the current script.
    /// </summary>
    public CSharpOptions AppendScript(Func<ExpressionExecutionContext, string> script)
    {
        ConfigureScriptCallbacks.Add((s, c) => s.ContinueWith(script(c)));
        return this;
    }
}