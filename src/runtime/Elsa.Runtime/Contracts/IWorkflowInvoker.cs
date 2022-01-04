using Elsa.Models;
using Elsa.Runtime.Models;

namespace Elsa.Runtime.Contracts;

public interface IWorkflowInvoker
{
    Task<ExecuteWorkflowResult> ExecuteAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> ExecuteAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default);
    Task<DispatchWorkflowResult> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<DispatchWorkflowResult> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default);
}