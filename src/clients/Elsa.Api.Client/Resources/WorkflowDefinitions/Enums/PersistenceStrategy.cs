namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;

/// <summary>
/// Define the Persistence strategy to store information
/// </summary>
public enum PersistenceStrategy
{
    /// <summary>
    /// Persist using the Parent strategy
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