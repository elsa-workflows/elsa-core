using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Resilience.Models;
using Elsa.Workflows;

namespace Elsa.Resilience;

public class ResilienceService : IResilienceService
{
    private const string ResilienceStrategyIdPropKey = "resilienceStrategy";
    private readonly Lazy<Task<IEnumerable<IResilienceStrategy>>> _strategies;
    private readonly IEnumerable<IResilienceStrategyProvider> _providers;
    private readonly IExpressionEvaluator _expressionEvaluator;

    public ResilienceService(IEnumerable<IResilienceStrategyProvider> providers, IExpressionEvaluator expressionEvaluator)
    {
        _providers = providers;
        _expressionEvaluator = expressionEvaluator;
        _strategies = new(GetStrategiesInternalAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task<IEnumerable<IResilienceStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default)
    {
        return _strategies.Value;
    }

    private async Task<IEnumerable<IResilienceStrategy>> GetStrategiesInternalAsync()
    {
        var strategies = new List<IResilienceStrategy>();
        foreach (var provider in _providers) strategies.AddRange(await provider.GetStrategiesAsync());
        return strategies;
    }

    public async Task<T> ExecuteAsync<T>(IResilientActivity activity, ActivityExecutionContext context, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategyConfig = GetStrategyConfig(activity);
        var strategy = await EvaluateStrategyConfigAsync(context, strategyConfig, cancellationToken);
        return strategy == null ? await action() : await strategy.ExecuteAsync(action);
    }

    private ResilienceStrategyConfig? GetStrategyConfig(IResilientActivity resilientActivity)
    {
        return !resilientActivity.CustomProperties.TryGetValue(ResilienceStrategyIdPropKey, out var value)
            ? null
            : value.ConvertTo<ResilienceStrategyConfig>();
    }

    private async Task<IResilienceStrategy?> EvaluateStrategyConfigAsync(ActivityExecutionContext context, ResilienceStrategyConfig? config, CancellationToken cancellationToken = default)
    {
        if (config == null) return null;

        var mode = config.Mode;

        switch (mode)
        {
            case ResilienceStrategyConfigMode.Identifier:
                {
                    var strategyId = config.StrategyId;
                    return string.IsNullOrWhiteSpace(strategyId)
                        ? null
                        : (await GetStrategiesAsync(cancellationToken)).FirstOrDefault(x => x.Id == strategyId);
                }
            case ResilienceStrategyConfigMode.Expression:
                {
                    var expression = config.Expression;

                    if (expression == null)
                        return null;

                    var result = await _expressionEvaluator.EvaluateAsync<object>(expression, context.ExpressionExecutionContext, ExpressionEvaluatorOptions.Empty);

                    return result switch
                    {
                        string strategyId => (await GetStrategiesAsync(cancellationToken)).FirstOrDefault(x => x.Id == strategyId),
                        IResilienceStrategy strategy => strategy,
                        _ => null
                    };
                }
            default:
                return null;
        }
    }
}