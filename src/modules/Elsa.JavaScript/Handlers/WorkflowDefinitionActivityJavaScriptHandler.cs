using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
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

        // Create workflow input accessors.
        CreateWorkflowInputAccessors(engine, context);
        
        return Task.CompletedTask;
    }

    private void CreateWorkflowInputAccessors(Engine engine, ExpressionExecutionContext context)
    {
        if(context.TryGetWorkflowExecutionContext(out var workflowExecutionContext))
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
            // Typically, a workflow definition might have variables declared, that we want to be able to access from JavaScript expressions.
            foreach(var block in context.Memory.Blocks.Values)
            {
                if(block.Metadata is not VariableBlockMetadata variableBlockMetadata)
                    continue;
                
                var variable = variableBlockMetadata.Variable;
                var variablePascaleName = variable.Name.Pascalize();
                engine.SetValue($"get{variablePascaleName}", (Func<object?>)(() => block.Value));
            }
        }
    }
}