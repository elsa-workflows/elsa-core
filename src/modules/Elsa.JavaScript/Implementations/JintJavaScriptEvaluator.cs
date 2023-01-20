using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.Options;
using Elsa.JavaScript.Services;
using Elsa.Mediator.Services;
using Humanizer;
using Jint;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Implementations;

/// <summary>
/// Provides a JavaScript evaluator using Jint.
/// </summary>
public class JintJavaScriptEvaluator : IJavaScriptEvaluator
{
    private readonly IEventPublisher _mediator;
    private readonly JintOptions _jintOptions;

    /// <summary>
    /// Constructor.
    /// </summary>
    public JintJavaScriptEvaluator(IEventPublisher mediator, IOptions<JintOptions> scriptOptions)
    {
        _mediator = mediator;
        _jintOptions = scriptOptions.Value;
    }

    /// <inheritdoc />
    public async Task<object?> EvaluateAsync(string expression,
        Type returnType,
        ExpressionExecutionContext context,
        Action<Engine>? configureEngine = default,
        CancellationToken cancellationToken = default)
    {
        var engine = await GetConfiguredEngine(configureEngine, context, cancellationToken);
        var result = ExecuteExpressionAndGetResult(engine, expression);

        return result;
    }

    private async Task<Engine> GetConfiguredEngine(Action<Engine>? configureEngine, ExpressionExecutionContext context, CancellationToken cancellationToken)
    {
        var engine = new Engine(opts =>
        {
            if (_jintOptions.AllowClrAccess)
                opts.AllowClr();
        });

        configureEngine?.Invoke(engine);
            
        // Add common functions.
        engine.SetValue("setVariable", (Action<string, object>)((name, value) => context.SetVariable(name, value)));
            
        // ReSharper disable once ConvertClosureToMethodGroup (Jint does not understand method groups).
        engine.SetValue("getVariable", (Func<string, object?>)(name => context.GetVariable(name)));
            
        // Create variable setters and getters for each variable.
        CreateVariableAccessors(engine, context);
        
        engine.SetValue("isNullOrWhiteSpace", (Func<string, bool>)string.IsNullOrWhiteSpace);
        engine.SetValue("isNullOrEmpty", (Func<string, bool>)string.IsNullOrEmpty);
        engine.SetValue("toJson", (Func<object, string>)(value => JsonSerializer.Serialize(value)));

        // Add common .NET types.
        engine.RegisterType<DateTime>();
        engine.RegisterType<DateTimeOffset>();
        engine.RegisterType<TimeSpan>();

        // Allow listeners invoked by the mediator to configure the engine.
        await _mediator.PublishAsync(new EvaluatingJavaScript(engine, context), cancellationToken);

        return engine;
    }

    private static void CreateVariableAccessors(Engine engine, ExpressionExecutionContext context)
    {
        var variablesDictionary = context.GetVariableValues();

        foreach (var variable in variablesDictionary)
        {
            var pascalName = variable.Key.Pascalize();
            engine.SetValue($"get{pascalName}", (Func<object?>)(() => context.GetVariable(variable.Key)));
            engine.SetValue($"set{pascalName}", (Action<object?>)(value => context.SetVariable(variable.Key, value)));
        }
    }

    private static object? ExecuteExpressionAndGetResult(Engine engine, string expression)
    {
        var result = engine.Evaluate(expression);
        return result.ToObject();
    }
}