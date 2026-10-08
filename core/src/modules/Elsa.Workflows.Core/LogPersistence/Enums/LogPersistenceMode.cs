using JetBrains.Annotations;

namespace Elsa.Workflows.LogPersistence;

/// <summary>
/// Define the Log Persistence mode to store information
/// </summary>
[UsedImplicitly]
public enum LogPersistenceMode
{
    /// <summary>
    /// Persist using the Parent mode
    /// </summary>
    Inherit,

    /// <summary>
    /// Include property to store
    /// </summary>
    Include,

    /// <summary>
    /// Exclude Property to store
    /// </summary>
    Exclude
}