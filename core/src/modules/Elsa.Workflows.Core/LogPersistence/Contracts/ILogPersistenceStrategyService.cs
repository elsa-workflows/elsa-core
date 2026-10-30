namespace Elsa.Workflows.LogPersistence;

public interface ILogPersistenceStrategyService
{
    IEnumerable<ILogPersistenceStrategy> ListStrategies();
}