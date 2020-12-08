using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IWorkflowInstanceApi
    {
        [Get("/v1/workflow-instance/{id}")]
        Task<WorkflowInstance?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        
        [Get("/v1/workflow-instance")]
        Task<PagedList<WorkflowInstance>> ListAsync(int? page = default, int? pageSize = default, CancellationToken cancellationToken = default);
    }
}