using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
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
    

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionActivityJavaScriptHandler(IActivityRegistry activityRegistry, IExpressionEvaluator expressionEvaluator)
    {
        _activityRegistry = activityRegistry;
    }

    /// <inheritdoc />
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        var context = notification.Context;

        // Always create workflow input accessors.
        CreateWorkflowInputAccessors(engine, context);
        
        // Create input getters.
        //CreateCompositeActivityInputAccessors(engine, context);
        
        return Task.CompletedTask;
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

    // private void CreateCompositeActivityInputAccessors(Engine engine, ExpressionExecutionContext context)
    // {
    //     var workflowDefinitionActivity = context.GetActivityExecutionContext().GetFirstWorkflowDefinitionActivity();
    //     
    //     if (workflowDefinitionActivity == null)
    //         return;
    //
    //     var workflowDefinitionActivityDescriptor = _activityRegistry.Find(workflowDefinitionActivity.Type, workflowDefinitionActivity.Version);
    //     var inputDescriptors = workflowDefinitionActivityDescriptor?.Inputs ?? Enumerable.Empty<InputDescriptor>();
    //
    //     foreach (var inputDefinition in inputDescriptors)
    //     {
    //         var inputPascalName = inputDefinition.Name.Pascalize();
    //         var input = workflowDefinitionActivity.SyntheticProperties.TryGetValue(inputDefinition.Name, out var inputValue) ? (Input?)inputValue : default;
    //         var evaluatedExpression = input != null ? context.Get(input.MemoryBlockReference()) : default;
    //
    //         engine.SetValue($"get{inputPascalName}", (Func<object?>)(() => evaluatedExpression));
    //     }
    // }
}