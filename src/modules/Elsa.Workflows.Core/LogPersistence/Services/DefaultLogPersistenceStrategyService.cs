using JetBrains.Annotations;

namespace Elsa.Workflows.LogPersistence;

[UsedImplicitly]
public class DefaultLogPersistenceStrategyService(IEnumerable<ILogPersistenceStrategy> strategies) : ILogPersistenceStrategyService
{
    public IEnumerable<ILogPersistenceStrategy> ListStrategies()
    {
        return strategies;
    }
}