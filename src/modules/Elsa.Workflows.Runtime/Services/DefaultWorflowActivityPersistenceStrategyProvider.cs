using Elsa.Workflows.Enums;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Represents the default Persistence Strategy.
/// </summary>
public class DefaultWorkflowActivityPersistenceStrategyProvider
    : IWorkflowActivityPersistenceStrategyProvider
{
    PersistenceStrategy _defaultStrategy = PersistenceStrategy.Include;

    /// <summary>
    /// Set the default strategy to Include data
    /// </summary>
    public DefaultWorkflowActivityPersistenceStrategyProvider()
    { }

    /// <summary>
    /// Set the default strategy
    /// </summary>
    /// <param name="defaultStrategy">the default persistence strategy to configure</param>
    public DefaultWorkflowActivityPersistenceStrategyProvider(PersistenceStrategy defaultStrategy)
    {
        _defaultStrategy = defaultStrategy;
    }

    /// <inheritdoc/>
    public PersistenceStrategy GetGlobalPersistenceStrategy()
    {
        return _defaultStrategy;
    }
}