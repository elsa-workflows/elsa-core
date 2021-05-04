using System.Threading.Tasks;
using Elsa.Client.Models;

namespace ElsaDashboard.Shared.Rpc
{
    public static class WorkflowRegistryServiceExtensions
    {
        public static Task<PagedList<WorkflowBlueprintSummary>> ListAsync(this IWorkflowRegistryService service, int? page = default, int? pageSize = default, VersionOptions? versionOptions = default) =>
            service.ListAsync(new ListWorkflowBlueprintsRequest(page, pageSize, versionOptions));

        public static Task<WorkflowBlueprint?> GetById(this IWorkflowRegistryService service, string id, VersionOptions versionOptions) => service.GetById(new GetWorkflowBlueprintByIdRequest(id, versionOptions));
    }
}