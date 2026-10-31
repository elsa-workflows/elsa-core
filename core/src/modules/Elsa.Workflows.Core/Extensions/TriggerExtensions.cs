using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="Trigger"/>.
/// </summary>
public static class TriggerExtensions
{
    /// <summary>
    /// Returns a filtered list of triggers that match the specified activity type.
    /// </summary>
    /// <param name="triggers">The triggers to filter.</param>
    /// <typeparam name="T">The type of the activity.</typeparam>
    public static IEnumerable<Trigger> Filter<T>(this IEnumerable<Trigger> triggers) where T : IActivity
    {
        var activityTypeName = TypeNameHelper.GenerateTypeName<T>();
        return triggers.Where(x => x.Type == activityTypeName);
    }

    /// <summary>
    /// Creates an expression execution context for the specified trigger.
    /// </summary>
    /// <param name="trigger">The trigger for which to create an expression execution context.</param>
    /// <param name="activityDescriptor">The activity descriptor.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="context">The workflow indexing context.</param>
    /// <param name="expressionEvaluator">The expression evaluator.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>An expression execution context.</returns>
    public static async Task<ExpressionExecutionContext> CreateExpressionExecutionContextAsync(
        this ITrigger trigger,
        ActivityDescriptor activityDescriptor,
        IServiceProvider serviceProvider,
        WorkflowIndexingContext context,
        IExpressionEvaluator expressionEvaluator,
        ILogger logger)
    {
        var namedInputs = trigger.GetNamedInputs();
        var assignedInputs = namedInputs.Where(x => x.Value.MemoryBlockReference != null!).ToList();
        var register = context.GetOrCreateRegister(trigger);
        var cancellationToken = context.CancellationToken;
        var expressionInput = new Dictionary<string, object>();
        var applicationProperties = ExpressionExecutionContextExtensions.CreateTriggerIndexingPropertiesFrom(context.Workflow, expressionInput);
        applicationProperties[ExpressionExecutionContextExtensions.ActivityKey] = trigger;
        var expressionExecutionContext = new ExpressionExecutionContext(serviceProvider, register, null, applicationProperties, null, cancellationToken);

        // Evaluate activity inputs before requesting trigger data.
        foreach (var namedInput in assignedInputs)
        {
            var inputDescriptor = activityDescriptor.Inputs.FirstOrDefault(x => x.Name == namedInput.Key);

            if (inputDescriptor == null)
            {
                logger.LogWarning("Input descriptor not found for input '{InputName}'", namedInput.Key);
                continue;
            }

            if (!inputDescriptor.AutoEvaluate)
            {
                logger.LogDebug("Skipping input '{InputName}' because it is not set to auto-evaluate.", namedInput.Key);
                continue;
            }

            var input = namedInput.Value;
            var locationReference = input.MemoryBlockReference();

            if (locationReference.Id == null!)
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