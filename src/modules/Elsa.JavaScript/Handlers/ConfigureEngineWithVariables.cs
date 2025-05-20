using System.Dynamic;
using System.Text.RegularExpressions;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Helpers;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.Options;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using JetBrains.Annotations;
using Jint.Native;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Handlers;

/// <summary>
/// A handler that configures the Jint engine with workflow variables.
/// </summary>
[UsedImplicitly]
public partial class ConfigureEngineWithVariables(IOptions<JintOptions> options) : INotificationHandler<EvaluatingJavaScript>, INotificationHandler<EvaluatedJavaScript>
{
    private bool IsEnabled => options.Value is { DisableWrappers: false, DisableVariableCopying: false };

    /// <inheritdoc />
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
            return Task.CompletedTask;

        CopyVariablesIntoEngine(notification);
        return Task.CompletedTask;
    }

    public Task HandleAsync(EvaluatedJavaScript notification, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
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
        var expression = notification.Expression;
        var variableNames = GetUsedVariableNames(context, expression).ToList();
        var variablesContainer = (IDictionary<string, object?>)new ExpandoObject();

        foreach (var variableName in variableNames)
        {
            var variableValue = context.GetVariableInScope(variableName);
            variableValue = ObjectConverterHelper.ProcessVariableValue(engine, variableValue);
            variablesContainer[variableName] = variableValue;
        }

        engine.SetValue("variables", variablesContainer);
    }

    private IEnumerable<string> GetUsedVariableNames(ExpressionExecutionContext context, string expression)
    {
        var variableNames = context.GetVariableNamesInScope().FilterInvalidVariableNames();

        var variableNamesInScript = ExtractVariableNamesRegex().Matches(expression)
            .Select(m => m.Groups[1].Value)
            .ToList();
        
        return variableNames.Where(x => variableNamesInScript.Contains(x));
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

    [GeneratedRegex(@"variables\.(\w+)(?:\.\w+)*")]
    private static partial Regex ExtractVariableNamesRegex();
}