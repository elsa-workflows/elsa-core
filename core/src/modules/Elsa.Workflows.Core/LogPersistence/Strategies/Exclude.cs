using JetBrains.Annotations;

namespace Elsa.Workflows.LogPersistence.Strategies;

/// <summary>
/// A log persistence strategy that returns <see cref="LogPersistenceMode.Exclude"/>.
/// </summary>
[UsedImplicitly]
public class Exclude : ILogPersistenceStrategy
{
    public Task<LogPersistenceMode> GetPersistenceModeAsync(LogPersistenceStrategyContext context)
    {
        return Task.FromResult(LogPersistenceMode.Exclude);
    }
}