using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [Service]
    public interface IWorkflowInstanceService
    {
        [Operation]
        Task<PagedList<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CallContext context = default);

        [Operation]
        Task<WorkflowInstance?> GetByIdAsync(GetWorkflowInstanceByIdRequest request, CallContext context = default);

        [Operation]
        Task DeleteAsync(DeleteWorkflowInstanceRequest request, CallContext context = default);
    }
}