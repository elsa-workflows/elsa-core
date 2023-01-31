using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using Refit;

namespace Elsa.Client.Services
{
    public interface IWorkflowInstancesApi
    {
        [Get("/v1/workflow-instances/{id}")]
        Task<WorkflowInstance?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        [Get("/v1/workflow-instances")]
        Task<PagedList<WorkflowInstanceSummary>> ListAsync(
            int? page = default,
            int? pageSize = default,
            [AliasAs("workflow")] string? workflowDefinitionId = default,
            [AliasAs("status")] WorkflowStatus? workflowStatus = default,
            WorkflowInstanceOrderBy? orderBy = default,
            string? searchTerm = default,
            CancellationToken cancellationToken = default);

        [Delete("/v1/workflow-instances/{id}")]
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        [Delete("/v1/workflow-instances/bulk")]
        Task BulkDeleteAsync([Body] BulkDeleteWorkflowInstancesRequest instancesRequest, CancellationToken cancellationToken = default);

        [Post("/v1/workflow-instances/{id}/retry")]
        Task RetryAsync(string id, RetryWorkflowRequest request, CancellationToken cancellationToken = default);

        [Post("/v1/workflow-instances/bulk/retry")]
        Task BulkRetryAsync([Body] BulkRetryWorkflowInstancesRequest instancesRequest, CancellationToken cancellationToken = default);
    }
}