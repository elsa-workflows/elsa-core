using System.Threading.Tasks;
using Elsa.Client;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using ProtoBuf.Grpc;

namespace ElsaDashboard.Backend.Rpc
{
    public class WorkflowRegistryService : IWorkflowRegistryService
    {
        private readonly IElsaClient _elsaClient;
        public WorkflowRegistryService(IElsaClient elsaClient) => _elsaClient = elsaClient;

        public Task<PagedList<WorkflowBlueprintSummary>> ListAsync(ListWorkflowBlueprintsRequest request, CallContext context = default) =>
            _elsaClient.WorkflowRegistry.ListAsync(request.Page, request.PageSize, request.VersionOptions, context.CancellationToken);

        public async Task<WorkflowBlueprint?> GetById(GetWorkflowBlueprintByIdRequest request, CallContext context = default)
        {
            var result = await _elsaClient.WorkflowRegistry.GetByIdAsync(request.Id, request.VersionOptions, context.CancellationToken);
            return result;
        }
    }
}