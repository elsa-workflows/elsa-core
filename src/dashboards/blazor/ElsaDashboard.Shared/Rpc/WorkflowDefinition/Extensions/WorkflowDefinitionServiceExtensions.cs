using System.Threading.Tasks;
using Elsa.Client.Models;

namespace ElsaDashboard.Shared.Rpc
{
    public static class WorkflowDefinitionServiceExtensions
    {
        public static async Task<PagedList<WorkflowDefinitionSummary>> ListAsync(this IWorkflowDefinitionService service, int? page = default, int? pageSize = default, VersionOptions? versionOptions = default) => await service.ListAsync(new ListWorkflowDefinitionsRequest(page, pageSize, versionOptions));
        public static async Task<WorkflowDefinition> GetByIdAsync(this IWorkflowDefinitionService service, string workflowDefinitionId, VersionOptions? versionOptions = default) => await service.GetByIdAsync(new GetWorkflowDefinitionByIdRequest(workflowDefinitionId, versionOptions));
        public static async Task<WorkflowDefinition> GetByVersionIdAsync(this IWorkflowDefinitionService service, string workflowDefinitionVersionId) => await service.GetByVersionIdAsync(new GetWorkflowDefinitionByVersionIdRequest(workflowDefinitionVersionId));
        public static async Task DeleteAsync(this IWorkflowDefinitionService service, string workflowDefinitionId) => await service.DeleteAsync(new DeleteWorkflowDefinitionRequest(workflowDefinitionId));
    }
}