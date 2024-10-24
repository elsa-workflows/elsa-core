using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.LogPersistence.Strategies;

[UsedImplicitly]
public class Configuration(IOptions<LogPersistenceOptions> options) : ILogPersistenceStrategy
{
    public Task<LogPersistenceMode> ShouldPersistAsync(LogPersistenceStrategyContext context)
    {
        return Task.FromResult(options.Value.ConfiguredPersistenceMode);
    }
}