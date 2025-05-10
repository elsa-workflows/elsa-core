using Elsa.Expressions.Helpers;
using Elsa.Resilience.Models;
using Elsa.Workflows;

namespace Elsa.Resilience;

public class ResilientActivityInvoker(IResilienceStrategyConfigEvaluator configEvaluator) : IResilientActivityInvoker
{
    private const string ResilienceStrategyIdPropKey = "resilienceStrategy";

    public async Task<T> InvokeAsync<T>(IResilientActivity activity, ActivityExecutionContext context, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategyConfig = GetStrategyConfig(activity);
        var strategy = await configEvaluator.EvaluateAsync(strategyConfig, context.ExpressionExecutionContext, cancellationToken);
        return strategy == null ? await action() : await strategy.ExecuteAsync(action);
    }

    private ResilienceStrategyConfig? GetStrategyConfig(IResilientActivity resilientActivity)
    {
        return !resilientActivity.CustomProperties.TryGetValue(ResilienceStrategyIdPropKey, out var value)
            ? null
            : value.ConvertTo<ResilienceStrategyConfig>();
    }
}