namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;

/// <summary>
/// Defines the order by options for workflow definitions.
/// </summary>
public enum OrderByWorkflowDefinition
{
    /// <summary>
    /// Order by the date the workflow definition was created.
    /// </summary>
    Created,

    /// <summary>
    /// Order by the name of the workflow definition.
    /// </summary>
    Name,
    
    /// <summary>
    /// Order by the version of the workflow definition.
    /// </summary>
    Version
}