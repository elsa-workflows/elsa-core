using System.Reflection;

namespace Elsa.Resilience;

public class ResilienceStrategyCatalog : IResilienceStrategyCatalog
{
    private readonly Lazy<Task<IEnumerable<IResilienceStrategy>>> _strategies;
    private readonly IEnumerable<IResilienceStrategySource> _sources;

    public ResilienceStrategyCatalog(IEnumerable<IResilienceStrategySource> sources)
    {
        _sources = sources;
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
        foreach (var source in _sources)
        {
            var sourceType = source.GetType();
            var sourceNameAttr = sourceType.GetCustomAttribute<ResilienceSourceNameAttribute>();
            var providerPrefix = sourceNameAttr?.Name ?? sourceType.Name;
            var providedStrategies = (await source.GetStrategiesAsync()).ToList();
            
            foreach (var strategy in providedStrategies)
                strategy.Id = $"{providerPrefix}:{strategy.Id}";
            
            strategies.AddRange(providedStrategies);
        }
        return strategies;
    }
}