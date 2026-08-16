using System.Diagnostics.CodeAnalysis;
using Acornima.Ast;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.JavaScript.Helpers;
using Elsa.Expressions.JavaScript.Notifications;
using Elsa.Expressions.JavaScript.ObjectConverters;
using Elsa.Expressions.JavaScript.Options;
using Elsa.Mediator.Contracts;
using Jint;
using Jint.Runtime.Interop;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// ReSharper disable ConvertClosureToMethodGroup
namespace Elsa.Expressions.JavaScript.Services;

/// <summary>
/// Provides a JavaScript evaluator using Jint.
/// </summary>
public class JintJavaScriptEvaluator(IConfiguration configuration, INotificationSender mediator, IOptions<JintOptions> scriptOptions, IMemoryCache memoryCache)
    : IJavaScriptEvaluator
{
    // The converters are stateless, so a single instance of each can serve every engine.
    private static readonly IObjectConverter[] ObjectConverters = [new ByteArrayConverter(), new EnumToStringConverter(), new JsonElementConverter()];

    private readonly JintOptions _jintOptions = scriptOptions.Value;

    /// <inheritdoc />
    [RequiresUnreferencedCode("The Jint library uses reflection and can't be statically analyzed.")]
    public async Task<object?> EvaluateAsync(string expression,
        Type returnType,
        ExpressionExecutionContext context,
        ExpressionEvaluatorOptions? options = null,
        Action<Engine>? configureEngine = null,
        CancellationToken cancellationToken = default)
    {
        var engine = await GetConfiguredEngine(configureEngine, context, options, cancellationToken);
        await mediator.SendAsync(new EvaluatingJavaScript(engine, context, expression), cancellationToken);
        var result = await ExecuteExpressionAndGetResultAsync(engine, expression, cancellationToken);
        await mediator.SendAsync(new EvaluatedJavaScript(engine, context, expression, result), cancellationToken);

        return result.ConvertTo(returnType);
    }

    private async Task<Engine> GetConfiguredEngine(Action<Engine>? configureEngine, ExpressionExecutionContext context, ExpressionEvaluatorOptions? options, CancellationToken cancellationToken)
    {
        options ??= new();

        var engineOptions = new Jint.Options
        {
            ExperimentalFeatures = ExperimentalFeature.TaskInterop
        };

        // Jint 4.14 changed this default to LiveView, which exposes a CLR array to script as a live view over
        // the original array rather than as a copy. Keeping the copy semantics means a script that mutates an
        // array does not reach back into the workflow's own data, and that an array survives a round trip as
        // object[] the way it always has. Hosts that want the live view can opt in via ConfigureEngineOptions.
        engineOptions.Interop.ArrayConversion = ArrayConversionMode.Copy;

        ConfigureClrAccess(engineOptions);
        ConfigureObjectWrapper(engineOptions);
        ConfigureObjectConverters(engineOptions);

        await mediator.SendAsync(new CreatingJavaScriptEngine(engineOptions, context), cancellationToken);
        _jintOptions.ConfigureEngineOptionsCallback(engineOptions, context);

        var engine = new Engine(engineOptions);

        configureEngine?.Invoke(engine);
        ConfigureArgumentGetters(engine, options);
        ConfigureConfigurationAccess(engine);
        _jintOptions.ConfigureEngineCallback(engine, context);

        return engine;
    }

    private void ConfigureClrAccess(Jint.Options options)
    {
        if (_jintOptions.AllowClrAccess)
            options.AllowClr();
    }

    private void ConfigureObjectWrapper(Jint.Options options)
    {
        options.SetWrapObjectHandler((engine, target, type) =>
        {
            var instance = ObjectWrapper.Create(engine, target);

            if (ObjectArrayHelper.DetermineIfObjectIsArrayLikeClrCollection(target.GetType()))
                instance.Prototype = engine.Intrinsics.Array.PrototypeObject;

            return instance;
        });
    }

    private void ConfigureObjectConverters(Jint.Options options)
    {
        options.Interop.ObjectConverters.AddRange(ObjectConverters);
    }

    private void ConfigureArgumentGetters(Engine engine, ExpressionEvaluatorOptions options)
    {
        foreach (var argument in options.Arguments)
            engine.SetValue($"get{argument.Key}", (Func<object?>)(() => argument.Value));
    }

    private void ConfigureConfigurationAccess(Engine engine)
    {
        if (_jintOptions.AllowConfigurationAccess)
            engine.SetValue("getConfig", (Func<string, object?>)(name => configuration.GetSection(name).Value));
    }

    private async Task<object?> ExecuteExpressionAndGetResultAsync(Engine engine, string expression, CancellationToken cancellationToken)
    {
        var preparedScript = GetOrCreatePrepareScript(expression);

        // EvaluateAsync awaits a returned promise instead of blocking the calling thread on it, which matters
        // for expressions that await a .NET Task, such as the ones calling getSecret().
        var result = await engine.EvaluateAsync(preparedScript, cancellationToken);
        return result.ToObject();
    }

    private Prepared<Script> GetOrCreatePrepareScript(string expression)
    {
        // The key type keeps these entries distinct from any other consumer of the shared cache, so the
        // expression itself can be used as the key. A cache hit then costs a dictionary lookup rather than
        // a hash of the entire expression plus the allocations needed to render that hash as a string.
        var cacheKey = new ScriptCacheKey(expression);

        // Looking the entry up directly rather than through GetOrCreate keeps the factory closure off the
        // hot path: it is only needed on a miss.
        if (memoryCache.TryGetValue(cacheKey, out Prepared<Script> cachedScript))
            return cachedScript;

        using var entry = memoryCache.CreateEntry(cacheKey);

        if (_jintOptions.ScriptCacheTimeout.HasValue)
            entry.SetSlidingExpiration(_jintOptions.ScriptCacheTimeout.Value);

        var preparedScript = PrepareScript(expression);
        entry.Value = preparedScript;
        return preparedScript;
    }

    private Prepared<Script> PrepareScript(string expression)
    {
        var prepareOptions = new ScriptPreparationOptions
        {
            ParsingOptions = new()
            {
                AllowReturnOutsideFunction = true
            }
        };
        return Engine.PrepareScript(expression, options: prepareOptions);
    }

    /// <summary>
    /// Identifies a prepared script in the shared memory cache.
    /// </summary>
    private readonly record struct ScriptCacheKey(string Expression);
}