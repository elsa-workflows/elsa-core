namespace Elsa.Api.Client.Resources.WorkflowInstances.Enums;

/// <summary>
/// Defines the order by options for workflow instances.
/// </summary>
public enum OrderByWorkflowInstance
{
    /// <summary>
    /// Order by the date the workflow instance was created.
    /// </summary>
    Created,

    /// <summary>
    /// Order by the date the workflow instance was last executed.
    /// </summary>
    LastExecuted,

    /// <summary>
    /// Order by the date the workflow instance was finished.
    /// </summary>
    Finished,

    /// <summary>
    /// Order by the name of the workflow instance.
    /// </summary>
    Name
}