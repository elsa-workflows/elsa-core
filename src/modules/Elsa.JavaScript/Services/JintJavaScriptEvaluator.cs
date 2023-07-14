using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.Options;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Memory;
using Humanizer;
using Jint;
using Microsoft.Extensions.Options;

// ReSharper disable ConvertClosureToMethodGroup

namespace Elsa.JavaScript.Services;

/// <summary>
/// Provides a JavaScript evaluator using Jint.
/// </summary>
public class JintJavaScriptEvaluator : IJavaScriptEvaluator
{
    private readonly INotificationSender _mediator;
    private readonly JintOptions _jintOptions;

    /// <summary>
    /// Constructor.
    /// </summary>
    public JintJavaScriptEvaluator(INotificationSender mediator, IOptions<JintOptions> scriptOptions)
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
        engine.SetValue("getWorkflowInstanceId", (Func<string>)(() => context.GetActivityExecutionContext().WorkflowExecutionContext.Id));
        engine.SetValue("setCorrelationId", (Action<string?>)(value => context.GetActivityExecutionContext().WorkflowExecutionContext.CorrelationId = value));
        engine.SetValue("getCorrelationId", (Func<string?>)(() => context.GetActivityExecutionContext().WorkflowExecutionContext.CorrelationId));
        engine.SetValue("setVariable", (Action<string, object>)((name, value) => SetVariableInScope(context, name, value)));
        engine.SetValue("getVariable", (Func<string, object?>)(name => GetVariableInScope(context, name)));
        engine.SetValue("getInput", (Func<string, object?>)(name => context.GetWorkflowExecutionContext().Input.GetValue(name)));
        engine.SetValue("getOutputFrom", (Func<string, string?, object?>)((activityIdOrNodeId, outputName) => GetOutput(context, activityIdOrNodeId, outputName)));
        engine.SetValue("getLastResult", (Func<object?>)(() => GetLastResult(context)));

        // Create variable getters and setters for each variable.
        CreateVariableAccessors(engine, context);

        // Create output getters for each activity.
        CreateOutputAccessors(engine, context);

        engine.SetValue("isNullOrWhiteSpace", (Func<string, bool>)(value => string.IsNullOrWhiteSpace(value)));
        engine.SetValue("isNullOrEmpty", (Func<string, bool>)(value => string.IsNullOrEmpty(value)));
        engine.SetValue("toJson", (Func<object, string>)(value => Serialize(value)));
        engine.SetValue("parseGuid", (Func<string, Guid>)(value => Guid.Parse(value)));
        engine.SetValue("newGuid", (Func<Guid>)(() => Guid.NewGuid()));
        engine.SetValue("newGuidString", (Func<string>)(() => Guid.NewGuid().ToString()));
        engine.SetValue("newShortGuid", (Func<string>)(() => Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "")));

        // Add common .NET types.
        engine.RegisterType<DateTime>();
        engine.RegisterType<DateTimeOffset>();
        engine.RegisterType<TimeSpan>();
        engine.RegisterType<Guid>();

        // Allow listeners invoked by the mediator to configure the engine.
        await _mediator.SendAsync(new EvaluatingJavaScript(engine, context), cancellationToken);

        return engine;
    }

    private static object? GetLastResult(ExpressionExecutionContext context)
    {
        var workflowExecutionContext = context.GetWorkflowExecutionContext();
        return workflowExecutionContext.GetLastActivityResult();
    }

    private static object? GetOutput(ExpressionExecutionContext context, string activityId, string? outputName)
    {
        var workflowExecutionContext = context.GetWorkflowExecutionContext();
        var outputRegister = workflowExecutionContext.GetActivityOutputRegister();
        return outputRegister.FindOutputByActivityId(activityId, outputName);
    }

    private static void CreateVariableAccessors(Engine engine, ExpressionExecutionContext context)
    {
        var variableNames = GetVariableNamesInScope(context).ToList();

        foreach (var variableName in variableNames)
        {
            var pascalName = variableName.Pascalize();
            engine.SetValue($"get{pascalName}", (Func<object?>)(() => GetVariableInScope(context, variableName)));
            engine.SetValue($"set{pascalName}", (Action<object?>)(value => SetVariableInScope(context, variableName, value)));
        }
    }

    private static IEnumerable<string> GetVariableNamesInScope(ExpressionExecutionContext context) =>
        EnumerateVariablesInScope(context)
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct();

    private static object GetVariableInScope(ExpressionExecutionContext context, string variableName)
    {
        var q = from variable in EnumerateVariablesInScope(context)
            where variable.Name == variableName
            where variable.TryGet(context, out _)
            select variable.Get(context);

        var value = q.FirstOrDefault();
        return value!;
    }

    private static void SetVariableInScope(ExpressionExecutionContext context, string variableName, object? value)
    {
        var q = from v in EnumerateVariablesInScope(context)
            where v.Name == variableName
            where v.TryGet(context, out _)
            select v;

        var variable = q.FirstOrDefault();
        variable?.Set(context, value);
    }

    private static IEnumerable<Variable> EnumerateVariablesInScope(ExpressionExecutionContext context)
    {
        var currentScope = context;

        while (currentScope != null)
        {
            if (!currentScope.TryGetActivityExecutionContext(out var activityExecutionContext))
                break;

            var variables = activityExecutionContext.Variables;

            foreach (var variable in variables)
                yield return variable;

            currentScope = currentScope.ParentContext;
        }
    }

    private static void CreateOutputAccessors(Engine engine, ExpressionExecutionContext context)
    {
        // Select activities with outputs.
        var activityExecutionContext = context.GetActivityExecutionContext();
        var activitiesWithOutputs = activityExecutionContext.GetActivitiesWithOutputs();

        foreach (var activityWithOutput in activitiesWithOutputs)
        {
            var activity = activityWithOutput.Activity;
            var activityDescriptor = activityWithOutput.ActivityDescriptor;

            foreach (var output in activityDescriptor.Outputs)
            {
                var outputPascalName = output.Name.Pascalize();
                var activityIdPascalName = activity.Id.Pascalize();
                engine.SetValue($"get{outputPascalName}From{activityIdPascalName}", (Func<object?>)(() => GetOutput(context, activity.Id, output.Name)));
            }
        }
    }

    private static object ExecuteExpressionAndGetResult(Engine engine, string expression)
    {
        var result = engine.Evaluate(expression);
        return result.ToObject();
    }

    private static string Serialize(object value)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());

        return JsonSerializer.Serialize(value, options);
    }
}