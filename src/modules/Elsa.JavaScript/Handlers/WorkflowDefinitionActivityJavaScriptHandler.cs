using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Extensions;
using Humanizer;
using JetBrains.Annotations;
using Jint;

namespace Elsa.JavaScript.Handlers;

/// <summary>
/// Configures the JavaScript engine with workflow input getters.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionActivityJavaScriptHandler : INotificationHandler<EvaluatingJavaScript>
{
    private readonly IActivityRegistry _activityRegistry;
    private readonly IExpressionEvaluator _expressionEvaluator;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionActivityJavaScriptHandler(IActivityRegistry activityRegistry, IExpressionEvaluator expressionEvaluator)
    {
        _activityRegistry = activityRegistry;
        _expressionEvaluator = expressionEvaluator;
    }

    /// <inheritdoc />
    public async Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        var context = notification.Context;

        // Always create workflow input accessors.
        CreateWorkflowInputAccessors(engine, context);
        
        // If we are already evaluating inputs, then we're in a circular evaluation loop. In this case, we should not attempt to evaluate the inputs.
        if (context.TransientProperties.TryGetValue("EvaluatingInputs", out var evaluatingInputs) && (bool)evaluatingInputs)
            return;

        // To prevent a circular evaluation loop, set a flag on the context to indicate that we're currently evaluating the inputs.
        context.TransientProperties["EvaluatingInputs"] = true;

        // Create input getters.
        await CreateInputAccessorsAsync(engine, context);
        
        // Remove the flag from the context.
        context.TransientProperties.Remove("EvaluatingInputs");
    }

    private void CreateWorkflowInputAccessors(Engine engine, ExpressionExecutionContext context)
    {
        var input = context.GetWorkflowExecutionContext().Input;

        foreach (var inputEntry in input)
        {
            var inputPascalName = inputEntry.Key.Pascalize();
            var inputValue = inputEntry.Value;
            engine.SetValue($"get{inputPascalName}", (Func<object?>)(() => inputValue));
        }
    }

    private async Task CreateInputAccessorsAsync(Engine engine, ExpressionExecutionContext context)
    {
        var workflowDefinitionActivity = context.GetActivityExecutionContext().GetFirstWorkflowDefinitionActivity();
        
        if (workflowDefinitionActivity == null)
            return;

        var workflowDefinitionActivityDescriptor = _activityRegistry.Find(workflowDefinitionActivity.Type, workflowDefinitionActivity.Version);
        var inputDefinitions = workflowDefinitionActivityDescriptor?.Inputs ?? Enumerable.Empty<InputDescriptor>();

        foreach (var inputDefinition in inputDefinitions)
        {
            var inputPascalName = inputDefinition.Name.Pascalize();
            var input = workflowDefinitionActivity.SyntheticProperties.TryGetValue(inputDefinition.Name, out var inputValue) ? (Input?)inputValue : default;
            var evaluatedExpression = input != null ? await _expressionEvaluator.EvaluateAsync(input, context) : input;

            engine.SetValue($"get{inputPascalName}", (Func<object?>)(() => evaluatedExpression));
        }
    }
}