using Elsa.Resilience.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Resilience.Providers;

public class ConfigurationResilienceStrategyProvider(IOptions<ResilienceStrategiesOptions> options) : IResilienceStrategyProvider
{
    public Task<IEnumerable<IResilienceStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<IResilienceStrategy>>(options.Value.ResilienceStrategies);
    }
}