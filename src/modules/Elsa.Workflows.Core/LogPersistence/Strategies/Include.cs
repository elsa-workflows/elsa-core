using JetBrains.Annotations;

namespace Elsa.Workflows.LogPersistence.Strategies;

[UsedImplicitly]
public class Include : ILogPersistenceStrategy
{
    public Task<LogPersistenceMode> ShouldPersistAsync(LogPersistenceStrategyContext context)
    {
        return Task.FromResult(LogPersistenceMode.Include);
    }
}