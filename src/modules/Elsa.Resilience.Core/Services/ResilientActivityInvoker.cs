using Elsa.Expressions.Helpers;
using Elsa.Resilience.Models;
using Elsa.Workflows;
using Polly;

namespace Elsa.Resilience;

public class ResilientActivityInvoker(IResilienceStrategyConfigEvaluator configEvaluator) : IResilientActivityInvoker
{
    private const string ResilienceStrategyIdPropKey = "resilienceStrategy";

    public async Task<T> InvokeAsync<T>(IResilientActivity activity, ActivityExecutionContext context, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategyConfig = GetStrategyConfig(activity);
        var strategy = await configEvaluator.EvaluateAsync(strategyConfig, context.ExpressionExecutionContext, cancellationToken);
        var resilienceContext = new Context
        {
            {
                nameof(ActivityExecutionContext), context
            }
        };
        return strategy == null ? await action() : await strategy.ExecuteAsync(c => action(), resilienceContext);
    }

    private ResilienceStrategyConfig? GetStrategyConfig(IResilientActivity resilientActivity)
    {
        return !resilientActivity.CustomProperties.TryGetValue(ResilienceStrategyIdPropKey, out var value)
            ? null
            : value.ConvertTo<ResilienceStrategyConfig>();
    }
}