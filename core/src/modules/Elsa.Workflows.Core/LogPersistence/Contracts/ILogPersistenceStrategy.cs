namespace Elsa.Workflows.LogPersistence;

public interface ILogPersistenceStrategy
{
    Task<LogPersistenceMode> GetPersistenceModeAsync(LogPersistenceStrategyContext context);
}