namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;

/// <summary>
/// Define the Log Persistence mode to store information
/// </summary>
public enum LogPersistenceMode
{
    /// <summary>
    /// Persist using the Parent mode
    /// </summary>
    Default,

    /// <summary>
    /// Include property to store
    /// </summary>
    Include,

    /// <summary>
    /// Exclude Property to store
    /// </summary>
    Exclude

}