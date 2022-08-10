using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IWorkflowDefinitionsApi
    {
        [Get("/{tenant}/v1/workflow-definitions/{workflowDefinitionId}/{versionOptions}")]
        Task<WorkflowDefinition> GetByIdAsync(string tenant, string workflowDefinitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
        
        [Get("/{tenant}/v1/workflow-definitions/{workflowDefinitionVersionId}")]
        Task<WorkflowDefinition> GetByVersionIdAsync(string tenant, string workflowDefinitionVersionId, CancellationToken cancellationToken = default);

        [Get("/{tenant}/v1/workflow-definitions")]
        Task<PagedList<WorkflowDefinitionSummary>> ListAsync(string tenant, int? page = default, int? pageSize = default, VersionOptions? version = default, CancellationToken cancellationToken = default);

        [Post("/{tenant}/v1/workflow-definitions")]
        Task<WorkflowDefinition> SaveAsync(string tenant, [Body] SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);

        [Delete("/{tenant}/v1/workflow-definitions/{workflowDefinitionId}")]
        Task DeleteAsync(string tenant, string workflowDefinitionId, CancellationToken cancellationToken = default);
    }
}