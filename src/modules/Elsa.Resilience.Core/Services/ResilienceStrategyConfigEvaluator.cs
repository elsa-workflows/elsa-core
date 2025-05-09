using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Resilience.Models;

namespace Elsa.Resilience;

public class ResilienceStrategyConfigEvaluator(IResilienceStrategyCatalog catalog, IExpressionEvaluator expressionEvaluator) : IResilienceStrategyConfigEvaluator
{
    public async Task<IResilienceStrategy?> EvaluateAsync(ResilienceStrategyConfig? config, ExpressionExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (config == null) return null;

        var mode = config.Mode;

        return mode switch
        {
            ResilienceStrategyConfigMode.Identifier => await ResolveByIdentifierAsync(config, cancellationToken),
            ResilienceStrategyConfigMode.Expression => await ResolveByExpressionAsync(config, context, cancellationToken),
            _ => null
        };
    }

    private async Task<IResilienceStrategy?> ResolveByIdentifierAsync(ResilienceStrategyConfig config, CancellationToken cancellationToken = default)
    {
        var strategyId = config.StrategyId;
        return string.IsNullOrWhiteSpace(strategyId)
            ? null
            : await catalog.GetAsync(strategyId, cancellationToken);
    }

    private async Task<IResilienceStrategy?> ResolveByExpressionAsync(ResilienceStrategyConfig config, ExpressionExecutionContext context, CancellationToken cancellationToken = default)
    {
        var expression = config.Expression;

        if (expression == null)
            return null;

        var result = await expressionEvaluator.EvaluateAsync<object>(expression, context, ExpressionEvaluatorOptions.Empty);

        return result switch
        {
            string strategyId => await catalog.GetAsync(strategyId, cancellationToken),
            IResilienceStrategy strategy => strategy,
            _ => null
        };
    }
}