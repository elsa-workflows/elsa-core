using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.Options;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Memory;
using Humanizer;
using Jint;
using Jint.Runtime;
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

        return result.ConvertTo(returnType);
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
        engine.SetValue("setVariable", (Action<string, object>)((name, value) => context.SetVariableInScope(name, value)));
        engine.SetValue("getVariable", (Func<string, object?>)(name => context.GetVariableInScope(name)));
        engine.SetValue("getInput", (Func<string, object?>)(name => GetInput(context, name)));
        engine.SetValue("getOutputFrom", (Func<string, string?, object?>)((activityIdName, outputName) => GetOutput(context, activityIdName, outputName)));
        engine.SetValue("getLastResult", (Func<object?>)(() => GetLastResult(context)));

        // Create variable getters and setters for each variable.
        CreateVariableAccessors(engine, context);

        // Create workflow input accessors - only if the current activity is not part of a composite activity definition.
        // Otherwise, the workflow input accessors will hide the composite activity input accessors which rely on variable accessors created above.
        if (!IsInsideCompositeActivity(context))
            CreateInputAccessors(engine, context);

        // Create output getters for each activity.
        CreateActivityOutputAccessors(engine, context);

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

    private static bool IsInsideCompositeActivity(ExpressionExecutionContext context)
    {
        if (!context.TryGetActivityExecutionContext(out var activityExecutionContext))
            return false;

        // If the first workflow definition in the ancestor hierarchy and that workflow definition has a parent, then we are inside a composite activity.
        var firstWorkflowContext = activityExecutionContext.GetAncestors().FirstOrDefault(x => x.Activity is Workflow);

        return firstWorkflowContext?.ParentActivityExecutionContext != null;
    }

    private static object? GetLastResult(ExpressionExecutionContext context)
    {
        var workflowExecutionContext = context.GetWorkflowExecutionContext();
        return workflowExecutionContext.GetLastActivityResult();
    }

    private object? GetInput(ExpressionExecutionContext expressionExecutionContext, string name)
    {
        // If there's a variable in the current scope with the specified name, return that.
        var variable = expressionExecutionContext.GetVariable(name);

        if (variable != null)
            return variable.Get(expressionExecutionContext);

        // Otherwise, return the input.
        var workflowExecutionContext = expressionExecutionContext.GetWorkflowExecutionContext();
        var input = workflowExecutionContext.Input;
        return input.TryGetValue(name, out var value) ? value : default;
    }

    private static object? GetOutput(ExpressionExecutionContext context, string activityIdOrName, string? outputName)
    {
        var workflowExecutionContext = context.GetWorkflowExecutionContext();
        var activityExecutionContext = context.GetActivityExecutionContext();
        var activity = activityExecutionContext.FindActivityByIdOrName(activityIdOrName);

        if (activity == null)
            throw new JavaScriptException("Activity not found.");

        var outputRegister = workflowExecutionContext.GetActivityOutputRegister();
        var outputRecordCandidates = outputRegister.FindMany(x => x.ActivityId == activity.Id && x.OutputName == outputName).ToList();
        var containerIds = activityExecutionContext.GetAncestors().Select(x => x.Id).ToList();
        var filteredOutputRecordCandidates = outputRecordCandidates.Where(x => containerIds.Contains(x.ContainerId)).ToList();
        var outputRecord = filteredOutputRecordCandidates.FirstOrDefault();
        return outputRecord?.Value;
    }

    private void CreateInputAccessors(Engine engine, ExpressionExecutionContext context)
    {
        // Only create workflow input accessors if the current activity is not part of a composite activity definition.
        if (context.TryGetWorkflowExecutionContext(out var workflowExecutionContext))
        {
            var input = workflowExecutionContext.Input;

            foreach (var inputEntry in input)
            {
                var inputPascalName = inputEntry.Key.Pascalize();
                var inputValue = inputEntry.Value;
                engine.SetValue($"get{inputPascalName}", (Func<object?>)(() => inputValue));
            }
        }
        else
        {
            // We end up here when we are evaluating an expression during trigger indexing.
            // The scenario being that a workflow definition might have variables declared, that we want to be able to access from JavaScript expressions.
            foreach (var block in context.Memory.Blocks.Values)
            {
                if (block.Metadata is not VariableBlockMetadata variableBlockMetadata)
                    continue;

                var variable = variableBlockMetadata.Variable;
                var variablePascalName = variable.Name.Pascalize();
                engine.SetValue($"get{variablePascalName}", (Func<object?>)(() => block.Value));
            }
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
        // Select activities with outputs.
        var activityExecutionContext = context.GetActivityExecutionContext();
        var useActivityName = activityExecutionContext.WorkflowExecutionContext.Workflow.CreatedWithModernTooling();
        var activitiesWithOutputs = activityExecutionContext.GetActivitiesWithOutputs();

        if (useActivityName)
            activitiesWithOutputs = activitiesWithOutputs.Where(x => !string.IsNullOrWhiteSpace(x.Activity.Name));

        foreach (var activityWithOutput in activitiesWithOutputs)
        {
            var activity = activityWithOutput.Activity;
            var activityDescriptor = activityWithOutput.ActivityDescriptor;

            foreach (var output in activityDescriptor.Outputs)
            {
                var outputPascalName = output.Name.Pascalize();
                var activityIdentifier = useActivityName ? activity.Name : activity.Id;
                var activityIdPascalName = activityIdentifier.Pascalize();
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