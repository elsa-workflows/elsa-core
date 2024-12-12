using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management;

/// <summary>
/// Extension methods for <see cref="WorkflowDefinition"/>.
/// </summary>
public static class WorkflowDefinitionExtensions
{
    /// <summary>
    /// Returns true if the workflow definition was created with modern tooling (i.e. Elsa Studio 3.0+).
    /// </summary>
    public static bool CreatedWithModernTooling(this WorkflowDefinition workflowDefinition) => workflowDefinition.ToolVersion?.Major >= 3;
    
    
    /// <summary>
    /// Looks for a <see cref="Workflow"/> by the specified version ID.
    /// </summary>
    public static async Task<Workflow?> FindWorkflowAsync(this IWorkflowDefinitionService service, string definitionVersionId, CancellationToken cancellationToken = default)
    {
        return (await service.FindWorkflowGraphAsync(definitionVersionId, cancellationToken: cancellationToken))?.Workflow;
    }
}