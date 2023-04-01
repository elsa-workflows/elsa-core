using Elsa.Api.Client.Models;

namespace Elsa.Api.Client.Contracts;

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
    Task<ListWorkflowDefinitionsResponse> ListAsync(ListWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a workflow definition.
    /// </summary>
    /// <param name="request">The request containing the ID of the workflow definition to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    Task<DeleteWorkflowDefinitionResponse> DeleteAsync(DeleteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
}