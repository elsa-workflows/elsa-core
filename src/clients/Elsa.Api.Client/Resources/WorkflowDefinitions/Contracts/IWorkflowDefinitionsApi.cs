using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using JetBrains.Annotations;
using Refit;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;

/// <summary>
/// Represents a client for the workflow definitions API.
/// </summary>
[PublicAPI]
public interface IWorkflowDefinitionsApi
{
    /// <summary>
    /// Lists workflow definitions.
    /// </summary>
    /// <param name="request">The request containing options for listing workflow definitions.</param>
    /// <param name="versionOptions">The version options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/workflow-definitions?versionOptions={versionOptions}")]
    Task<ListWorkflowDefinitionsResponse> ListAsync([Query]ListWorkflowDefinitionsRequest request, [Query]VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workflow definition.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition to get.</param>
    /// <param name="versionOptions">The version options.</param>
    /// <param name="includeCompositeRoot">Whether to include the root activity of composite activities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/workflow-definitions/{definitionId}?versionOptions={versionOptions}&includeCompositeRoot={includeCompositeRoot}")]
    Task<WorkflowDefinition?> GetAsync(string definitionId, VersionOptions? versionOptions = default, bool includeCompositeRoot = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the number of workflow definitions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/workflow-definitions/query/count")]
    Task<CountWorkflowDefinitionsResponse> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether a workflow definition name is unique.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="definitionId">The ID of the workflow definition to exclude from the check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/workflow-definitions/validation/is-name-unique?name={name}")]
    Task<GetIsNameUniqueResponse> GetIsNameUniqueAsync(string name, string? definitionId = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a workflow definition.
    /// </summary>
    /// <param name="request">The request containing the workflow definition to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/workflow-definitions")]
    Task<WorkflowDefinition> SaveAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a workflow definition.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Delete("/workflow-definitions/{definitionId}")]
    Task<DeleteWorkflowDefinitionResponse> DeleteAsync(string definitionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes many workflow definitions.
    /// </summary>
    /// <param name="request">The request containing the IDs of the workflow definitions to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/bulk-actions/delete/workflow-definitions/by-definition-id")]
    Task<DeleteManyWorkflowDefinitionsResponse> DeleteManyAsync(DeleteManyWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default);
}