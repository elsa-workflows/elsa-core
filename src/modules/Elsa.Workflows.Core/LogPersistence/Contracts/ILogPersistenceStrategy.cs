namespace Elsa.Workflows.LogPersistence;

public interface ILogPersistenceStrategy
{
    Task<LogPersistenceMode> ShouldPersistAsync(LogPersistenceStrategyContext context);
}