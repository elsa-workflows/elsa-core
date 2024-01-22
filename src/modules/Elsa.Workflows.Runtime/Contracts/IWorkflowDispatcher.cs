using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Posts a message to a queue to invoke a specified workflow or trigger a set of workflows.
/// </summary>
public interface IWorkflowDispatcher
{
    /// <summary>
    /// Dispatches a request to execute the specified workflow definition.
    /// </summary>
    Task<DispatchWorkflowDefinitionResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dispatches a request to execute the specified workflow instance.
    /// </summary>
    Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Starts all workflows and resumes existing workflow instances based on the specified activity type and bookmark payload.
    /// </summary>
    Task<DispatchTriggerWorkflowsResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resumes the workflow waiting for the specified bookmark.
    /// </summary>
    Task<DispatchResumeWorkflowsResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default);
}