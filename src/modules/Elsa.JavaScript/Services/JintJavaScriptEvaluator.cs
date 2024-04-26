using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.Helpers;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.Options;
using Elsa.Mediator.Contracts;
using Esprima;
using Esprima.Ast;
using Humanizer;
using Jint;
using Jint.Runtime.Interop;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// ReSharper disable ConvertClosureToMethodGroup
namespace Elsa.JavaScript.Services;

/// <summary>
/// Provides a JavaScript evaluator using Jint.
/// </summary>
public class JintJavaScriptEvaluator : IJavaScriptEvaluator
{
    private readonly INotificationSender _mediator;
    private readonly IMemoryCache _memoryCache;
    private readonly JintOptions _jintOptions;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructor.
    /// </summary>
    public JintJavaScriptEvaluator(IConfiguration configuration, INotificationSender mediator, IOptions<JintOptions> scriptOptions, IMemoryCache memoryCache)
    {
        _mediator = mediator;
        _memoryCache = memoryCache;
        _jintOptions = scriptOptions.Value;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<object?> EvaluateAsync(string expression,
        Type returnType,
        ExpressionExecutionContext context,
        ExpressionEvaluatorOptions? options = default,
        Action<Engine>? configureEngine = default,
        CancellationToken cancellationToken = default)
    {
        var engine = await GetConfiguredEngine(configureEngine, context, options, cancellationToken);
        var result = ExecuteExpressionAndGetResult(engine, expression);

        return result.ConvertTo(returnType);
    }

    private async Task<Engine> GetConfiguredEngine(Action<Engine>? configureEngine, ExpressionExecutionContext context, ExpressionEvaluatorOptions? options, CancellationToken cancellationToken)
    {
        options ??= new ExpressionEvaluatorOptions();

        var engine = new Engine(opts =>
        {
            if (_jintOptions.AllowClrAccess)
                opts.AllowClr();

            // Wrap objects in ObjectWrapper instances and set their prototype to Array.prototype if they are array-like.
            opts.SetWrapObjectHandler((engine, target, type) =>
            {
                var instance = new ObjectWrapper(engine, target);

                if (ObjectArrayHelper.DetermineIfObjectIsArrayLikeClrCollection(target.GetType()))
                    instance.Prototype = engine.Intrinsics.Array.PrototypeObject;

                return instance;
            });
        });

        configureEngine?.Invoke(engine);

        // Add common functions.
        engine.SetValue("getWorkflowInstanceId", (Func<string>)(() => context.GetActivityExecutionContext().WorkflowExecutionContext.Id));
        engine.SetValue("setCorrelationId", (Action<string?>)(value => context.GetActivityExecutionContext().WorkflowExecutionContext.CorrelationId = value));
        engine.SetValue("getCorrelationId", (Func<string?>)(() => context.GetActivityExecutionContext().WorkflowExecutionContext.CorrelationId));
        engine.SetValue("setVariable", (Action<string, object>)((name, value) => context.SetVariableInScope(name, value)));
        engine.SetValue("getVariable", (Func<string, object?>)(name => context.GetVariableInScope(name)));
        engine.SetValue("getInput", (Func<string, object?>)(name => context.GetInput(name)));
        engine.SetValue("getOutputFrom", (Func<string, string?, object?>)((activityIdName, outputName) => context.GetOutput(activityIdName, outputName)));
        engine.SetValue("getLastResult", (Func<object?>)(() => context.GetLastResult()));

        // Create variable getters and setters for each variable.
        CreateVariableAccessors(engine, context);

        // Create workflow input accessors - only if the current activity is not part of a composite activity definition.
        // Otherwise, the workflow input accessors will hide the composite activity input accessors which rely on variable accessors created above.
        CreateWorkflowInputAccessors(engine, context);

        // Create output getters for each activity.
        CreateActivityOutputAccessors(engine, context);

        // Create argument getters for each argument.
        foreach (var argument in options.Arguments)
            engine.SetValue($"get{argument.Key}", (Func<object?>)(() => argument.Value));

        // Add common functions.
        engine.SetValue("isNullOrWhiteSpace", (Func<string, bool>)(value => string.IsNullOrWhiteSpace(value)));
        engine.SetValue("isNullOrEmpty", (Func<string, bool>)(value => string.IsNullOrEmpty(value)));
        engine.SetValue("toJson", (Func<object, string>)(value => Serialize(value)));
        engine.SetValue("parseGuid", (Func<string, Guid>)(value => Guid.Parse(value)));
        engine.SetValue("newGuid", (Func<Guid>)(() => Guid.NewGuid()));
        engine.SetValue("newGuidString", (Func<string>)(() => Guid.NewGuid().ToString()));
        engine.SetValue("newShortGuid", (Func<string>)(() => Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "")));

        // Deprecated, use newGuidString instead.
        engine.SetValue("getGuidString", (Func<string>)(() => Guid.NewGuid().ToString()));

        // Deprecated, use newShortGuid instead.
        engine.SetValue("getShortGuid", (Func<string>)(() => Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "")));

        // Create configuration value accessor
        if (_jintOptions.AllowConfigurationAccess)
            engine.SetValue("getConfig", (Func<string, object?>)(name => _configuration.GetSection(name).Value));

        // Add common .NET types.
        engine.RegisterType<DateTime>();
        engine.RegisterType<DateTimeOffset>();
        engine.RegisterType<TimeSpan>();
        engine.RegisterType<Guid>();

        // Invoke registered configuration callback.
        _jintOptions.ConfigureEngineCallback(engine, context);

        // Allow listeners invoked by the mediator to configure the engine.
        await _mediator.SendAsync(new EvaluatingJavaScript(engine, context), cancellationToken);

        return engine;
    }

    private void CreateWorkflowInputAccessors(Engine engine, ExpressionExecutionContext context)
    {
        if (context.IsInsideCompositeActivity())
            return;

        var inputs = context.GetWorkflowInputs().ToDictionary(x => x.Name);

        if (!context.TryGetWorkflowExecutionContext(out var workflowExecutionContext))
            return;

        var inputDefinitions = workflowExecutionContext.Workflow.Inputs;

        foreach (var inputDefinition in inputDefinitions)
        {
            var input = inputs.GetValueOrDefault(inputDefinition.Name);
            engine.SetValue($"get{inputDefinition.Name}", (Func<object?>)(() => input?.Value));
        }
    }

    private static void CreateVariableAccessors(Engine engine, ExpressionExecutionContext context)
    {
        var variableNames = context.GetVariableNamesInScope().ToList();

        foreach (var variableName in variableNames)
        {
            var pascalName = variableName.Pascalize();
            engine.SetValue($"get{pascalName}", (Func<object?>)(() => context.GetVariableInScope(variableName)));
            engine.SetValue($"set{pascalName}", (Action<object?>)(value => context.SetVariableInScope(variableName, value)));
        }
    }

    private static void CreateActivityOutputAccessors(Engine engine, ExpressionExecutionContext context)
    {
        var activityOutputs = context.GetActivityOutputs();

        foreach (var activityOutput in activityOutputs)
        foreach (var outputName in activityOutput.OutputNames)
            engine.SetValue($"get{outputName}From{activityOutput.ActivityName}", (Func<object?>)(() => context.GetOutput(activityOutput.ActivityId, outputName)));
    }

    private object? ExecuteExpressionAndGetResult(Engine engine, string expression)
    {
        var cacheKey = "jint:script:" + expression.GetHashCode(StringComparison.Ordinal);

        var parsedScript = _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            if (_jintOptions.ScriptCacheTimeout.HasValue)
                entry.SetAbsoluteExpiration(_jintOptions.ScriptCacheTimeout.Value);

            var parser = new JavaScriptParser(new ParserOptions { AllowReturnOutsideFunction = true });
            var script = parser.ParseScript(expression);
            return script;
        })!;

        var result = engine.Evaluate(parsedScript);
        return result.ToObject();
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private static string Serialize(object value)
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        options.Converters.Add(new JsonStringEnumConverter());

        return JsonSerializer.Serialize(value, options);
    }
}