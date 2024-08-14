using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

// ReSharper disable once CheckNamespace
namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Provides a set of extension methods for <see cref="IWorkflowDispatcher"/>.
/// </summary>
public static class WorkflowDispatcherExtensions
{
    /// <summary>
    /// Dispatches a request to execute the specified workflow definition.
    /// </summary>
    public static Task<DispatchWorkflowResponse> DispatchAsync(this IWorkflowDispatcher workflowDispatcher, DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        return workflowDispatcher.DispatchAsync(request, new DispatchWorkflowOptions(), cancellationToken);
    }

    /// <summary>
    /// Dispatches a request to execute the specified workflow instance.
    /// </summary>
    public static Task<DispatchWorkflowResponse> DispatchAsync(this IWorkflowDispatcher workflowDispatcher, DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        return workflowDispatcher.DispatchAsync(request, new DispatchWorkflowOptions(), cancellationToken);
    }

    /// <summary>
    /// Starts all workflows and resumes existing workflow instances based on the specified activity type and bookmark payload.
    /// </summary>
    public static Task<DispatchWorkflowResponse> DispatchAsync(this IWorkflowDispatcher workflowDispatcher, DispatchTriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        return workflowDispatcher.DispatchAsync(request, new DispatchWorkflowOptions(), cancellationToken);
    }

    /// <summary>
    /// Resumes the workflow waiting for the specified bookmark.
    /// </summary>
    public static Task<DispatchWorkflowResponse> DispatchAsync(this IWorkflowDispatcher workflowDispatcher, DispatchResumeWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        return workflowDispatcher.DispatchAsync(request, new DispatchWorkflowOptions(), cancellationToken);
    }
}