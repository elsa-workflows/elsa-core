using Elsa.Extensions;
using Elsa.Resilience.Serialization;
using Microsoft.Extensions.Configuration;

namespace Elsa.Resilience.Providers;

public class ConfigurationResilienceStrategyProvider(IConfiguration configuration, ResilienceStrategySerializer serializer) : IResilienceStrategyProvider
{
    public Task<IEnumerable<IResilienceStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetStrategies());
    }
    
    private IEnumerable<IResilienceStrategy> GetStrategies()
    {
        var json = configuration.GetSectionAsJson("Resilience:Strategies");
        return string.IsNullOrWhiteSpace(json) ? [] : serializer.DeserializeMany(json);
    }
}