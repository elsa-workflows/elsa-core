using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Helpers;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.Options;
using Elsa.Mediator.Contracts;
using Humanizer;
using JetBrains.Annotations;
using Jint;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Handlers;

/// <summary>
/// A handler that configures the Jint engine with workflow input and output accessors.
/// </summary>
[UsedImplicitly]
public class ConfigureEngineWithVariablesAndInputOutputAccessors(IOptions<JintOptions> options) : INotificationHandler<EvaluatingJavaScript>
{
    /// <inheritdoc />
    public async Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        if (options.Value.DisableWrappers)
            return;

        var engine = notification.Engine;
        var context = notification.Context;

        // The order of the next 3 lines is important.
        CreateVariableAccessors(engine, context);
        CreateWorkflowInputAccessors(engine, context);
        await CreateActivityOutputAccessorsAsync(engine, context);
    }

    private void CreateVariableAccessors(Engine engine, ExpressionExecutionContext context)
    {
        var variableNames = context.GetVariableNamesInScope().FilterInvalidVariableNames().ToList();

        foreach (var variableName in variableNames)
        {
            var pascalName = variableName.Pascalize();
            engine.SetValue($"get{pascalName}", (Func<object?>)(() => context.GetVariableInScope(variableName)));
            engine.SetValue($"set{pascalName}", (Action<object?>)(value =>
            {
                engine.SyncVariablesContainer(options, variableName, value);
                context.SetVariableInScope(variableName, value);
            }));
        }
    }

    private void CreateWorkflowInputAccessors(Engine engine, ExpressionExecutionContext context)
    {
        // Create workflow input accessors - only if the current activity is not part of a composite activity definition.
        // Otherwise, the workflow input accessors will hide the composite activity input accessors which rely on variable accessors.
        if (context.IsContainedWithinCompositeActivity())
            return;

        var inputs = context.GetWorkflowInputs().Where(x => x.Name.IsValidVariableName()).ToDictionary(x => x.Name);

        if (!context.TryGetWorkflowExecutionContext(out var workflowExecutionContext))
            return;

        var inputDefinitions = workflowExecutionContext.Workflow.Inputs;

        foreach (var inputDefinition in inputDefinitions)
        {
            var input = inputs.GetValueOrDefault(inputDefinition.Name);
            engine.SetValue($"get{inputDefinition.Name}", (Func<object?>)(() => input?.Value));
        }
    }

    private static async Task CreateActivityOutputAccessorsAsync(Engine engine, ExpressionExecutionContext context)
    {
        var activityOutputs = context.GetActivityOutputs();

        await foreach (var activityOutput in activityOutputs)
        foreach (var outputName in activityOutput.OutputNames.FilterInvalidVariableNames())
            engine.SetValue($"get{outputName}From{activityOutput.ActivityName}", (Func<object?>)(() => context.GetOutput(activityOutput.ActivityId, outputName)));
    }
}