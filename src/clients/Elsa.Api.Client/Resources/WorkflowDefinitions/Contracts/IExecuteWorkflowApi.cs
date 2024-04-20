using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using JetBrains.Annotations;
using Refit;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;

/// <summary>
/// Represents a client for the workflow definitions API.
/// </summary>
[PublicAPI]
public interface IExecuteWorkflowApi
{
    /// <summary>
    /// Executes a workflow definition.
    /// </summary>
    /// <param name="definitionId">The definition ID of the workflow definition to execute.</param>
    /// <param name="request">An optional request containing options for executing the workflow definition.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A response containing information about the workflow instance that was created.</returns>
    [Post("/workflow-definitions/{definitionId}/execute")]
    Task<HttpResponseMessage> ExecuteAsync(string definitionId, ExecuteWorkflowDefinitionRequest? request = null , CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dispatches a request to execute the specified workflow definition.
    /// </summary>
    /// <param name="definitionId">The definition ID of the workflow definition to dispatch request.</param>
    /// <param name="request">An optional request containing options for dispatching a request to execute the specified workflow definition.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A response containing information about the workflow instance that was created.</returns>
    [Post("/workflow-definitions/{definitionId}/dispatch")]
    Task<HttpResponseMessage> DispatchAsync(string definitionId, DispatchWorkflowDefinitionRequest? request = null, CancellationToken cancellationToken = default);
}