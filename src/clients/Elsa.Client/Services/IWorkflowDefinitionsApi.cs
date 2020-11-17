using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IWorkflowDefinitionsApi
    {
        [Get("/v1/workflow-definitions/{workflowDefinitionId}/{versionOptions}")]
        Task<WorkflowDefinition> GetByDefinitionAndVersionAsync(string workflowDefinitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
        
        [Get("/v1/workflow-definitions/{workflowDefinitionVersionId}")]
        Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default);

        [Get("/v1/workflow-definitions")]
        Task<PagedList<WorkflowDefinition>> ListAsync(int? page = default, int? pageSize = default, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);

        [Post("/v1/workflow-definitions")]
        Task<WorkflowDefinition> PostAsync([Body(BodySerializationMethod.Serialized)] PostWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);

        [Post("/v1/workflow-definitions/{workflowDefinitionId}")]
        Task<WorkflowDefinition> PostAsync(string workflowDefinitionId, [Body(BodySerializationMethod.Serialized)] PostWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    }
}