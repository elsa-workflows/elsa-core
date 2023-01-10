using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IWorkflowsApi
    {
        [Post("/v1/workflows/{workflowDefinitionId}/dispatch")]
        Task<WorkflowDispatchResponse> DispatchWorkflowAsync(string workflowDefinitionId, [Body] DispatchWorkflowDefinitionRequest? context, CancellationToken cancellationToken = default);
        
        [Post("/v1/workflows/{workflowDefinitionId}/execute")]
        Task<WorkflowExecutionResponse> ExecuteWorkflowAsync(string workflowDefinitionId, [Body] ExecuteWorkflowDefinitionRequest? context, CancellationToken cancellationToken = default);
    }
}