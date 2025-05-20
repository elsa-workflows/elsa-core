using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Scripting.CSharp.Contexts;
using Elsa.Scripting.CSharp.Models;
using Elsa.Expressions.Models;
using Microsoft.CodeAnalysis.Scripting;

namespace Elsa.Scripting.CSharp.Options;

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
        typeof(Globals).Assembly, // Elsa.Scripting.CSharp
        typeof(Enumerable).Assembly, // System.Linq
        typeof(Guid).Assembly, // System.Runtime
        typeof(JsonSerializer).Assembly, // System.Text.Json
        typeof(IDictionary<string, object>).Assembly, // System.Collections
    });

    /// <summary>
    /// A list of namespaces that will be imported when evaluating a C# expression.
    /// </summary>
    public ISet<string> Namespaces { get; } = new HashSet<string>(new[]
    {
        typeof(Globals).Namespace!, // Elsa.Scripting.CSharp
        typeof(Enumerable).Namespace!, // System.Linq
        typeof(Guid).Namespace!, // System
        typeof(JsonSerializer).Namespace!, // System.Text.Json
        typeof(JsonConverter).Namespace!, // System.Text.Json.Serialization
        typeof(JsonNode).Namespace!, // System.Text.Json.Nodes
        typeof(IDictionary<string, object>).Namespace!, // System.Collections.Generic
    });

    /// <summary>
    /// The timeout for script caching.
    /// </summary>
    /// <remarks>
    /// The <c>ScriptCacheTimeout</c> property specifies the duration for which the scripts are cached in the C# engine. When a script is executed, it is compiled and cached for future use. This caching improves performance by avoiding repetitive compilation of the same script.
    /// If the value of <c>ScriptCacheTimeout</c> is <c>null</c>, the scripts are cached indefinitely. If a time value is specified, the scripts will be purged from the cache after they've been unused for the specified duration and recompiled on next use.
    /// </remarks>
    public TimeSpan? ScriptCacheTimeout { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Disables the generation of variable wrappers. E.g. <c>Variables.MyVariable</c> will no longer be available for variables. Instead, you can only access variables using <c>Variables.Get("MyVariable")</c> and the typed <c>Variables.Get&lt;T&gt;("MyVariable")</c> function.
    /// This is useful if your application requires the use of invalid JavaScript variable names.
    /// </summary>
    public bool DisableWrappers { get; set; }

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