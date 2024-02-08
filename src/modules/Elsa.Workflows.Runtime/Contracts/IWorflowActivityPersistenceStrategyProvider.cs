using Elsa.Workflows.Enums;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents the default Persistence Strategy.
/// </summary>
public interface IWorkflowActivityPersistenceStrategyProvider
{
    /// <summary>
    /// Get the Global Persistence configuration
    /// </summary>
    /// <returns>the persistence strategy</returns>
    PersistenceStrategy GetGlobalPersistenceStrategy();
}
