using System.Dynamic;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Helpers;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.Options;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using JetBrains.Annotations;
using Jint;
using Jint.Native;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Handlers;

/// <summary>
/// A handler that configures the Jint engine with workflow variables.
/// </summary>
[UsedImplicitly]
public class ConfigureEngineWithVariables(IOptions<JintOptions> options) : INotificationHandler<EvaluatingJavaScript>, INotificationHandler<EvaluatedJavaScript>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        if (options.Value.DisableWrappers)
            return Task.CompletedTask;

        CopyVariablesIntoEngine(notification);
        return Task.CompletedTask;
    }

    public Task HandleAsync(EvaluatedJavaScript notification, CancellationToken cancellationToken)
    {
        if (options.Value.DisableWrappers)
            return Task.CompletedTask;

        CopyVariablesIntoWorkflowExecutionContext(notification);
        return Task.CompletedTask;
    }

    private void CopyVariablesIntoWorkflowExecutionContext(EvaluatedJavaScript notification)
    {
        var context = notification.Context;
        var engine = notification.Engine;
        var variablesContainer = (IDictionary<string, object?>)engine.GetValue("variables").ToObject()!;
        var inputNames = GetInputNames(context).FilterInvalidVariableNames().Distinct().ToList();

        foreach (var (variableName, variableValue) in variablesContainer)
        {
            if (inputNames.Contains(variableName))
                continue;

            var processedValue = variableValue is JsObject jsValue ? jsValue.ToObject() : variableValue ?? context.GetVariableInScope(variableName);
            context.SetVariable(variableName, processedValue);
        }
    }

    private void CopyVariablesIntoEngine(EvaluatingJavaScript notification)
    {
        var engine = notification.Engine;
        var context = notification.Context;
        var variableNames = context.GetVariableNamesInScope().FilterInvalidVariableNames().ToList();
        var variablesContainer = (IDictionary<string, object?>)new ExpandoObject();

        foreach (var variableName in variableNames)
        {
            var variableValue = context.GetVariableInScope(variableName);
            variableValue = ObjectConverterHelper.ProcessVariableValue(engine, variableValue);
            variablesContainer[variableName] = variableValue;
        }

        engine.SetValue("variables", variablesContainer);
    }

    private IEnumerable<string> GetInputNames(ExpressionExecutionContext context)
    {
        var activityExecutionContext = context.TryGetActivityExecutionContext(out var aec) ? aec : null;

        while (activityExecutionContext != null)
        {
            if (activityExecutionContext.Activity is Workflow workflow)
            {
                var inputDefinitions = workflow.Inputs;

                foreach (var inputDefinition in inputDefinitions)
                    yield return inputDefinition.Name;
            }

            foreach (var syntheticProperty in activityExecutionContext.Activity.SyntheticProperties)
            {
                yield return syntheticProperty.Key;
            }

            activityExecutionContext = activityExecutionContext.ParentActivityExecutionContext;
        }
    }
}