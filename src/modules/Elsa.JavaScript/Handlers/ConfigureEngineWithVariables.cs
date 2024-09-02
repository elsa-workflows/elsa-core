using System.Dynamic;
using Elsa.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.JavaScript.Handlers;

/// A handler that configures the Jint engine with workflow variables.
[UsedImplicitly]
public class ConfigureEngineWithVariables : INotificationHandler<EvaluatingJavaScript>, INotificationHandler<EvaluatedJavaScript>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        CopyVariablesIntoEngine(notification);
        return Task.CompletedTask;
    }
    
    public Task HandleAsync(EvaluatedJavaScript notification, CancellationToken cancellationToken)
    {
        CopyVariablesIntoWorkflowExecutionContext(notification);
        return Task.CompletedTask;
    }

    private void CopyVariablesIntoWorkflowExecutionContext(EvaluatedJavaScript notification)
    {
        var context = notification.Context;
        var engine = notification.Engine;
        var variablesContainer = (IDictionary<string, object?>)engine.GetValue("variables").ToObject()!;
        
        foreach (var (variableName, variableValue) in variablesContainer) 
            context.SetVariable(variableName, variableValue);
    }

    private void CopyVariablesIntoEngine(EvaluatingJavaScript notification)
    {
        var engine = notification.Engine;
        var context = notification.Context;
        var variableNames = context.GetVariableNamesInScope().ToList();
        var variablesContainer = (IDictionary<string, object?>)new ExpandoObject();
        
        foreach (var variableName in variableNames)
        {
            variablesContainer[variableName] = context.GetVariableInScope(variableName);
        }
        
        engine.SetValue("variables", variablesContainer);
    }
}