namespace Elsa.Resilience;

public class ResilienceService : IResilienceService
{
    private const string ResilienceStrategyId = "ResilienceStrategyId";
    private readonly Lazy<Task<IEnumerable<IResilienceStrategy>>> _strategies;
    private readonly IEnumerable<IResilienceStrategyProvider> _providers;

    public ResilienceService(IEnumerable<IResilienceStrategyProvider> providers)
    {
        _providers = providers;
        _strategies = new(GetStrategiesAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string? GetStrategyId(IResilientActivity resilientActivity)
    {
        return !resilientActivity.CustomProperties.TryGetValue(ResilienceStrategyId, out var strategyIdVal)
            ? null
            : strategyIdVal.ToString();
    }

    public async Task<IResilienceStrategy?> GetStrategyByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var strategies = await _strategies.Value;
        return strategies.FirstOrDefault(x => x.Id == id);
    }

    public Task<T> ExecuteAsync<T>(IResilientActivity activity, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async Task<IEnumerable<IResilienceStrategy>> GetStrategiesAsync()
    {
        var strategies = new List<IResilienceStrategy>();
        foreach (var provider in _providers) strategies.AddRange(await provider.GetStrategiesAsync());
        return strategies;
    }
}