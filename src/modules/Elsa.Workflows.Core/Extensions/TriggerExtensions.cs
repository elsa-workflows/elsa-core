using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class TriggerExtensions
{
    public static IEnumerable<Trigger> Filter<T>(this IEnumerable<Trigger> triggers) where T : IActivity
    {
        var activityTypeName = TypeNameHelper.GenerateTypeName<T>();
        return triggers.Where(x => x.Type == activityTypeName);
    }
    
    public static async Task<ExpressionExecutionContext> CreateExpressionExecutionContextAsync(
        this ITrigger trigger, 
        IServiceProvider serviceProvider, 
        WorkflowIndexingContext context, 
        IExpressionEvaluator expressionEvaluator,
        ILogger logger)
    {
        var inputs = trigger.GetInputs();
        var assignedInputs = inputs.Where(x => x.MemoryBlockReference != null!).ToList();
        var register = context.GetOrCreateRegister(trigger);
        var cancellationToken = context.CancellationToken;
        var expressionInput = new Dictionary<string, object>();
        var applicationProperties = ExpressionExecutionContextExtensions.CreateTriggerIndexingPropertiesFrom(context.Workflow, expressionInput);
        var expressionExecutionContext = new ExpressionExecutionContext(serviceProvider, register, default, applicationProperties, cancellationToken);

        // Evaluate activity inputs before requesting trigger data.
        foreach (var input in assignedInputs)
        {
            var locationReference = input.MemoryBlockReference();

            if(locationReference.Id == null!)
                continue;
            
            try
            {
                var value = await expressionEvaluator.EvaluateAsync(input, expressionExecutionContext);
                locationReference.Set(expressionExecutionContext, value);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to evaluate '{@Expression}'", input.Expression);
            }
        }

        return expressionExecutionContext;
    }
}