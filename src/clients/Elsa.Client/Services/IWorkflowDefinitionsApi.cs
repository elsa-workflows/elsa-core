using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IWorkflowDefinitionsApi
    {
        [Get("/v1/workflow-definitions/{id}/latest")]
        Task<WorkflowDefinition> GetLatestAsync([AliasAs("id")] string workflowDefinitionId, CancellationToken cancellationToken = default);

        [Get("/v1/workflow-definitions/{id}/published")]
        Task<WorkflowDefinition> GetPublishedAsync([AliasAs("id")] string workflowDefinitionId, CancellationToken cancellationToken = default);
    }
}