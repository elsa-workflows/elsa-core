using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
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
            OrderBy? orderBy = default,
            string? searchTerm = default,
            CancellationToken cancellationToken = default);

        [Delete("/v1/workflow-instances/{id}")]
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}