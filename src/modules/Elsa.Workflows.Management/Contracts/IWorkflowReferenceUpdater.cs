using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Updates references to the specified workflow of all workflows that reference it.
/// </summary>
public interface IWorkflowReferenceUpdater
{
    /// <summary>
    /// Updates references to the specified workflow of all workflows that reference it.
    /// </summary>
    /// <param name="definition">The workflow definition to update references for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<UpdateWorkflowReferencesResult> UpdateWorkflowReferencesAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
}