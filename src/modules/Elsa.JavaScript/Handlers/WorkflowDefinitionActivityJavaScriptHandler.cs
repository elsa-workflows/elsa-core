using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Activities;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Services;
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

        // If we are already evaluating inputs, then we're in a circular evaluation loop. In this case, we should not attempt to evaluate the inputs.
        if(context.TransientProperties.TryGetValue("EvaluatingInputs", out var evaluatingInputs) && (bool)evaluatingInputs)
            return;
        
        // To prevent a circular evaluation loop, set a flag on the context to indicate that we're currently evaluating the inputs.
        context.TransientProperties["EvaluatingInputs"] = true;
        
        // Create input getters.
        await CreateInputAccessorsAsync(engine, context);
    }

    private async Task CreateInputAccessorsAsync(Engine engine, ExpressionExecutionContext context)
    {
        var workflowDefinitionActivity = GetFirstWorkflowDefinitionActivity(context);

        if (workflowDefinitionActivity == null)
            return;

        var descriptor = _activityRegistry.Find(workflowDefinitionActivity.Type, workflowDefinitionActivity.Version)!;
        var inputDefinitions = descriptor.Inputs;
        
        foreach (var inputDefinition in inputDefinitions)
        {
            var inputPascalName = inputDefinition.Name.Pascalize();
            var input = workflowDefinitionActivity.SyntheticProperties.TryGetValue(inputDefinition.Name, out var inputValue) ? (Input?)inputValue : default;
            var evaluatedExpression = input != null ? await _expressionEvaluator.EvaluateAsync(input, context) : input;

            engine.SetValue($"get{inputPascalName}", (Func<object?>)(() => evaluatedExpression));
        }
    }

    private static WorkflowDefinitionActivity? GetFirstWorkflowDefinitionActivity(ExpressionExecutionContext context) =>
        context.GetActivityExecutionContext().GetFirstWorkflowDefinitionActivity();
}