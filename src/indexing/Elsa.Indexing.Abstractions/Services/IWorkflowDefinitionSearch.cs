using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Indexing.Models;

namespace Elsa.Indexing.Services
{
    public interface IWorkflowDefinitionSearch
    {
        Task<List<WorkflowDefinitionIndexModel>> SearchAsync(string search,
            string? tenantId = null,
            bool? isEnabled = null,
            int from = 0,
            int take = 20,
            CancellationToken cancellationToken = default
        );
    }
}