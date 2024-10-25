using JetBrains.Annotations;

namespace Elsa.Workflows.LogPersistence.Strategies;

/// <summary>
/// A log persistence strategy that returns <see cref="LogPersistenceMode.Include"/>. 
/// </summary>
[UsedImplicitly]
public class Include : ILogPersistenceStrategy
{
    public Task<LogPersistenceMode> GetPersistenceModeAsync(LogPersistenceStrategyContext context)
    {
        return Task.FromResult(LogPersistenceMode.Include);
    }
}