namespace Elsa.Resilience;

public class ResilienceStrategyCatalog : IResilienceStrategyCatalog
{
    private readonly Lazy<Task<IEnumerable<IResilienceStrategy>>> _strategies;
    private readonly IEnumerable<IResilienceStrategySource> _providers;

    public ResilienceStrategyCatalog(IEnumerable<IResilienceStrategySource> providers)
    {
        _providers = providers;
        _strategies = new(GetStrategiesInternalAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }
    
    public Task<IEnumerable<IResilienceStrategy>> ListAsync(CancellationToken cancellationToken = default)
    {
        return _strategies.Value;
    }

    public async Task<IResilienceStrategy?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var strategies = await ListAsync(cancellationToken);
        return strategies.FirstOrDefault(x => x.Id == id);
    }

    private async Task<IEnumerable<IResilienceStrategy>> GetStrategiesInternalAsync()
    {
        var strategies = new List<IResilienceStrategy>();
        foreach (var provider in _providers) strategies.AddRange(await provider.GetStrategiesAsync());
        return strategies;
    }
}