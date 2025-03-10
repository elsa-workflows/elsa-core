using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Posts a message to a queue to invoke a specified workflow or trigger a set of workflows.
/// </summary>
public interface IWorkflowDispatcher
{
    /// <summary>
    /// Dispatches a request to execute the specified workflow definition.
    /// </summary>
    Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions options, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dispatches a request to execute the specified workflow instance.
    /// </summary>
    Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions options, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Starts all workflows and resumes existing workflow instances based on the specified activity type and bookmark payload.
    /// </summary>
    Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions options, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resumes the workflow waiting for the specified bookmark.
    /// </summary>
    Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions options, CancellationToken cancellationToken = default);
}