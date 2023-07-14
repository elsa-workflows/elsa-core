using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Imports a workflow definition.
/// </summary>
public interface IWorkflowDefinitionImporter
{
    /// <summary>
    /// Imports a workflow definition.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The imported workflow definition.</returns>
    Task<ImportWorkflowResult> ImportAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
}