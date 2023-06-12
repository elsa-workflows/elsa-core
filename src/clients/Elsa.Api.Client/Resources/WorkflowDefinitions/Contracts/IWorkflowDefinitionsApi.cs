using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
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
    /// <param name="versionOptions">The version options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response containing the workflow definitions.</returns>
    [Get("/workflow-definitions?versionOptions={versionOptions}")]
    Task<ListWorkflowDefinitionsResponse> ListAsync([Query]ListWorkflowDefinitionsRequest request, [Query]VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workflow definition.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition to get.</param>
    /// <param name="versionOptions">The version options.</param>
    /// <param name="includeCompositeRoot">Whether to include the root activity of composite activities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The workflow definition.</returns>
    [Get("/workflow-definitions/{definitionId}?versionOptions={VersionOptions}&includeCompositeRoot={includeCompositeRoot}")]
    Task<WorkflowDefinition?> GetAsync(string definitionId, VersionOptions? versionOptions = default, bool includeCompositeRoot = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a workflow definition.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    [Delete("/workflow-definitions/{definitionId}")]
    Task<DeleteWorkflowDefinitionResponse> DeleteAsync(string definitionId, CancellationToken cancellationToken = default);
}