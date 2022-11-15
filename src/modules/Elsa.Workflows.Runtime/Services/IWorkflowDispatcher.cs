using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Posts a message to a queue to invoke a specified workflow.
/// </summary>
public interface IWorkflowDispatcher
{
    Task<DispatchWorkflowDefinitionResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default);
}