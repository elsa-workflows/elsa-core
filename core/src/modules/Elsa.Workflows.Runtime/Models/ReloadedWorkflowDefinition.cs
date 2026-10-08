using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Represents a reloaded workflow definition with necessary properties for 
/// identification, versioning, and usability status as an activity.
/// </summary>
/// <param name="DefinitionId">The unique identifier for the workflow definition.</param>
/// <param name="DefinitionVersionId">The unique identifier for the specific version of the workflow definition.</param>
/// <param name="Version">The version number of the workflow definition.</param>
/// <param name="UsableAsActivity">Indicates whether the workflow definition can be used as an activity.</param>
public record ReloadedWorkflowDefinition(string DefinitionId, string DefinitionVersionId, int Version, bool UsableAsActivity)
{
    /// <summary>
    /// Creates an instance of <see cref="ReloadedWorkflowDefinition"/> from a given <see cref="WorkflowDefinition"/>.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition used to create the reloaded workflow definition.</param>
    /// <returns>A new instance of <see cref="ReloadedWorkflowDefinition"/>.</returns>
    public static ReloadedWorkflowDefinition FromDefinition(WorkflowDefinition workflowDefinition)
    {
        return new ReloadedWorkflowDefinition
        (
            workflowDefinition.DefinitionId,
            workflowDefinition.Id,
            workflowDefinition.Version,
            workflowDefinition.Options.UsableAsActivity ?? false
        );
    }
}