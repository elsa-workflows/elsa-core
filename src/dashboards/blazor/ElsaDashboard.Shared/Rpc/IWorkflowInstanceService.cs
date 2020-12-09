using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace ElsaDashboard.Shared.Rpc
{
    [Service]
    public interface IWorkflowInstanceService
    {
        Task<PagedList<WorkflowInstance>> ListAsync(int page = 0, int pageSize = 50, CallContext context = default);
        Task<WorkflowInstance?> GetById(string id, CallContext context = default);
    }
}