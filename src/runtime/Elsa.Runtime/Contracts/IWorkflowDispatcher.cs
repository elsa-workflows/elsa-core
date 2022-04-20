using Elsa.Runtime.Models;

namespace Elsa.Runtime.Contracts;

/// <summary>
/// Posts a message to a queue to invoke a specified workflow.
/// </summary>
public interface IWorkflowDispatcher
{
    Task<DispatchWorkflowDefinitionResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default);
}