using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;
using Jint;

namespace Elsa.JavaScript.Handlers;

/// <summary>
/// 
/// </summary>
[UsedImplicitly]
public class ConfigureEngineWithInputOutputAccessors : INotificationHandler<EvaluatingJavaScript>
{
    /// <inheritdoc />
    public async Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        var context = notification.Context;
        CreateWorkflowInputAccessors(engine, context);
        await CreateActivityOutputAccessorsAsync(engine, context);
    }
    
    private void CreateWorkflowInputAccessors(Engine engine, ExpressionExecutionContext context)
    {
        // Create workflow input accessors - only if the current activity is not part of a composite activity definition.
        // Otherwise, the workflow input accessors will hide the composite activity input accessors which rely on variable accessors.
        if (context.IsContainedWithinCompositeActivity())
            return;

        var inputs = context.GetWorkflowInputs().ToDictionary(x => x.Name);

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
        foreach (var outputName in activityOutput.OutputNames)
            engine.SetValue($"get{outputName}From{activityOutput.ActivityName}", (Func<object?>)(() => context.GetOutput(activityOutput.ActivityId, outputName)));
    }
}