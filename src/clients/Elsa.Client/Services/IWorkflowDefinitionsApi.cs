using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IWorkflowDefinitionsApi
    {
        [Get("/v1/workflow-definitions/{workflowDefinitionId}")]
        Task<WorkflowDefinition> GetAsync(string workflowDefinitionId, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
        
        [Get("/v1/workflow-definitions")]
        Task<WorkflowDefinition> ListAsync(string workflowDefinitionId, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
        
        [Post("/v1/workflow-definitions")]
        Task<WorkflowDefinition> PostAsync([Body(BodySerializationMethod.Serialized)]PostWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v1/workflow-definitions/{workflowDefinitionId}")]
        Task<WorkflowDefinition> PostAsync(string workflowDefinitionId, [Body(BodySerializationMethod.Serialized)]PostWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    }
}