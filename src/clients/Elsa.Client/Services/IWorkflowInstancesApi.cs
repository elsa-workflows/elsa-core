using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using Refit;

namespace Elsa.Client.Services
{
    public interface IWorkflowInstancesApi
    {
        [Get("/{tenant}/v1/workflow-instances/{id}")]
        Task<WorkflowInstance?> GetByIdAsync(string tenant, string id, CancellationToken cancellationToken = default);

        [Get("/{tenant}/v1/workflow-instances")]
        Task<PagedList<WorkflowInstanceSummary>> ListAsync(
            string tenant,
            int? page = default,
            int? pageSize = default,
            [AliasAs("workflow")] string? workflowDefinitionId = default,
            [AliasAs("status")] WorkflowStatus? workflowStatus = default,
            OrderBy? orderBy = default,
            string? searchTerm = default,
            CancellationToken cancellationToken = default);

        [Delete("/{tenant}/v1/workflow-instances/{id}")]
        Task DeleteAsync(string tenant, string id, CancellationToken cancellationToken = default);

        [Delete("/{tenant}/v1/workflow-instances/bulk")]
        Task BulkDeleteAsync(string tenant, [Body] BulkDeleteWorkflowInstancesRequest instancesRequest, CancellationToken cancellationToken = default);

        [Post("/{tenant}/v1/workflow-instances/{id}/retry")]
        Task RetryAsync(string tenant, string id, RetryWorkflowRequest request, CancellationToken cancellationToken = default);

        [Post("/{tenant}/v1/workflow-instances/bulk/retry")]
        Task BulkRetryAsync(string tenant, [Body] BulkRetryWorkflowInstancesRequest instancesRequest, CancellationToken cancellationToken = default);
    }
}