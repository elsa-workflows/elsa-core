using JetBrains.Annotations;

namespace Elsa.Workflows.LogPersistence.Strategies;

[UsedImplicitly]
public class Exclude : ILogPersistenceStrategy
{
    public Task<LogPersistenceMode> ShouldPersistAsync(LogPersistenceStrategyContext context)
    {
        return Task.FromResult(LogPersistenceMode.Exclude);
    }
}