using System.Security.Cryptography;
using System.Text;
using Elsa.CSharp.Contracts;
using Elsa.CSharp.Models;
using Elsa.CSharp.Notifications;
using Elsa.CSharp.Options;
using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.CSharp.Services;

/// <summary>
/// A C# expression evaluator using Roslyn.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CSharpEvaluator"/> class.
/// </remarks>
public class CSharpEvaluator(INotificationSender notificationSender, IOptions<CSharpOptions> scriptOptions, IMemoryCache memoryCache) : ICSharpEvaluator
{
    private readonly CSharpOptions _csharpOptions = scriptOptions.Value;

    /// <inheritdoc />
    public async Task<object?> EvaluateAsync(
        string expression,
        Type returnType,
        ExpressionExecutionContext context,
        ExpressionEvaluatorOptions options,
        Func<ScriptOptions, ScriptOptions>? configureScriptOptions = default,
        Func<Script<object>, Script<object>>? configureScript = default,
        CancellationToken cancellationToken = default)
    {
        var scriptOptions = ScriptOptions.Default.WithOptimizationLevel(OptimizationLevel.Release);

        if (configureScriptOptions != null)
            scriptOptions = configureScriptOptions(scriptOptions);

        var globals = new Globals(context, options.Arguments);
        var script = CSharpScript.Create("", scriptOptions, typeof(Globals));

        if (configureScript != null)
            script = configureScript(script);

        var notification = new EvaluatingCSharp(options, script, scriptOptions, context);
        await notificationSender.SendAsync(notification, cancellationToken);
        scriptOptions = notification.ScriptOptions;
        script = notification.Script.ContinueWith(expression, scriptOptions);
        var runner = GetCompiledScript(script);
        return await runner(globals, cancellationToken: cancellationToken);
    }

    private ScriptRunner<object> GetCompiledScript(Script<object> script)
    {
        var cacheKey = "csharp:script:" + Hash(script);

        return memoryCache.GetOrCreate(cacheKey, entry =>
        {
            if (_csharpOptions.ScriptCacheTimeout.HasValue)
                entry.SetSlidingExpiration(_csharpOptions.ScriptCacheTimeout.Value);

            return script.CreateDelegate();
        })!;
    }

    private static string Hash(Script<object> script)
    {
        var ms = new MemoryStream();
        using (var sw = new StreamWriter(ms, Encoding.UTF8))
        {
            for (Script current = script; current != null; current = current.Previous)
            {
                sw.WriteLine(current.Code);
            }
        }

        if (!ms.TryGetBuffer(out var segment))
        {
            segment = ms.ToArray();
        }

        var hash = SHA256.HashData(segment.AsSpan());
        return Convert.ToBase64String(hash);
    }
}