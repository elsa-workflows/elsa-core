namespace Elsa.Resilience;

public class ResilienceService : IResilienceService
{
    private const string ResilienceStrategyIdPropKey = "ResilienceStrategyId";
    private readonly Lazy<Task<IEnumerable<IResilienceStrategy>>> _strategies;
    private readonly IEnumerable<IResilienceStrategyProvider> _providers;

    public ResilienceService(IEnumerable<IResilienceStrategyProvider> providers)
    {
        _providers = providers;
        _strategies = new(GetStrategiesInternalAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string? GetStrategyId(IResilientActivity resilientActivity)
    {
        return !resilientActivity.CustomProperties.TryGetValue(ResilienceStrategyIdPropKey, out var strategyIdVal)
            ? null
            : strategyIdVal.ToString();
    }

    public Task<IEnumerable<IResilienceStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default)
    {
        return _strategies.Value;       
    }

    public async Task<IResilienceStrategy?> GetStrategyByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var strategies = await _strategies.Value;
        return strategies.FirstOrDefault(x => x.Id == id);
    }

    public async Task<T> ExecuteAsync<T>(IResilientActivity activity, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategyId = GetStrategyId(activity);
        var strategy = strategyId == null ? null : await GetStrategyByIdAsync(strategyId, cancellationToken);
        return strategy == null ? await action() : await strategy.ExecuteAsync(action);
    }

    private async Task<IEnumerable<IResilienceStrategy>> GetStrategiesInternalAsync()
    {
        var strategies = new List<IResilienceStrategy>();
        foreach (var provider in _providers) strategies.AddRange(await provider.GetStrategiesAsync());
        return strategies;
    }
}