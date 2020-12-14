using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace ElsaDashboard.Shared.Rpc
{
    [Service]
    public interface IWorkflowDefinitionService
    {
        [Operation]
        Task<PagedList<WorkflowDefinition>> ListAsync(ListWorkflowDefinitionsRequest request, CallContext context = default);

        [Operation]
        Task<WorkflowDefinition> GetByIdAsync(GetWorkflowDefinitionByIdRequest request, CallContext context = default);

        [Operation]
        Task<WorkflowDefinition> GetByVersionIdAsync(GetWorkflowDefinitionByVersionIdRequest request, CallContext context = default);

        [Operation]
        Task<WorkflowDefinition> SaveAsync(SaveWorkflowDefinitionRequest request, CallContext context = default);
    }
}