using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Posts a message to a topic to cancel a specified set of workflows.
/// </summary>
public interface IWorkflowCancellationDispatcher
{
    /// <summary>
    /// Cancels the specified workflow instances.
    /// </summary>
    Task<DispatchCancelWorkflowsResponse> DispatchAsync(DispatchCancelWorkflowRequest request, CancellationToken cancellationToken = default);
}