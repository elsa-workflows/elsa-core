using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Indexing.Models;
using Elsa.Models;

namespace Elsa.Indexing.Services
{
    public interface IWorkflowInstanceSearch
    {
        Task<List<WorkflowInstanceIndexModel>> SearchAsync(string search,
            string? contextType = null,
            string? contextId = null,
            string? definitionId = null,
            string? tenantId = null,
            WorkflowStatus? workflowStatus = null,
            string? correlationId = null,
            int from = 0,
            int take = 20,
            CancellationToken cancellationToken = default
        );

        Task<List<WorkflowInstanceIndexModel>> SearchAsync<TContext>(string search,
            string? contextId,
            string? definitionId = null,
            string? tenantId = null,
            WorkflowStatus? workflowStatus = null,
            string? correlationId = null,
            int from = 0,
            int take = 20,
            CancellationToken cancellationToken = default
        );
    }
}