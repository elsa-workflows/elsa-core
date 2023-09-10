using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IWorkflowRegistryApi
    {
        [Get("/v1/workflow-registry/{id}/{versionOptions}")]
        Task<WorkflowBlueprint?> GetByIdAsync(string id, VersionOptions versionOptions, CancellationToken cancellationToken = default);
        
        [Get("/v1/workflow-registry")]
        Task<IEnumerable<WorkflowBlueprintSummary>> ListAsync(int? page = default, int? pageSize = default, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
    }
}