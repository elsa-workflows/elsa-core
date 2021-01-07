using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace ElsaDashboard.Shared.Rpc
{
    [Service]
    public interface IWorkflowRegistryService
    {
        [Operation]
        Task<PagedList<WorkflowBlueprintSummary>> ListAsync(ListWorkflowBlueprintsRequest request, CallContext context = default);

        [Operation]
        Task<WorkflowBlueprint?> GetById(GetWorkflowBlueprintByIdRequest request, CallContext context = default);
    }
}