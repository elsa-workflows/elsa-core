using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IWorkflowRegistryApi
    {
        [Get("/{tenant}/v1/workflow-registry/{id}/{versionOptions}")]
        Task<WorkflowBlueprint?> GetByIdAsync(string tenant, string id, VersionOptions versionOptions, CancellationToken cancellationToken = default);
        
        [Get("/{tenant}/v1/workflow-registry")]
        Task<PagedList<WorkflowBlueprintSummary>> ListAsync(string tenant, int? page = default, int? pageSize = default, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
    }
}