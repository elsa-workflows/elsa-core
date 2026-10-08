using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.LogPersistence.Strategies;

/// <summary>
/// A log persistence strategy that uses the configured persistence mode.
/// </summary>
[UsedImplicitly]
public class Configuration(IOptions<LogPersistenceOptions> options) : ILogPersistenceStrategy
{
    public Task<LogPersistenceMode> GetPersistenceModeAsync(LogPersistenceStrategyContext context)
    {
        return Task.FromResult(options.Value.ConfiguredPersistenceMode);
    }
}