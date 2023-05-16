using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Refit;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;

/// <summary>
/// Represents a client for the workflow definitions API.
/// </summary>
public interface IWorkflowDefinitionsApi
{
    /// <summary>
    /// Lists workflow definitions.
    /// </summary>
    /// <param name="request">The request containing options for listing workflow definitions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response containing the workflow definitions.</returns>
    [Get("/workflow-definitions")]
    Task<ListWorkflowDefinitionsResponse> ListAsync(ListWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a workflow definition.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    [Delete("/workflow-definitions/{definitionId}")]
    Task<DeleteWorkflowDefinitionResponse> DeleteAsync(string definitionId, CancellationToken cancellationToken = default);
}