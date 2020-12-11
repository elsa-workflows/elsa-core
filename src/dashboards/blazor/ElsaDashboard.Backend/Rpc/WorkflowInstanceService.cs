using System.Threading.Tasks;
using Elsa.Client;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using ProtoBuf.Grpc;

namespace ElsaDashboard.Backend.Rpc
{
    public class WorkflowInstanceService : IWorkflowInstanceService
    {
        private readonly IElsaClient _elsaClient;
        public WorkflowInstanceService(IElsaClient elsaClient) => _elsaClient = elsaClient;
        public Task<PagedList<WorkflowInstance>> ListAsync(ListWorkflowInstancesRequest request, CallContext context = default) => _elsaClient.WorkflowInstances.ListAsync(request.Page, request.PageSize, context.CancellationToken);
        public Task<WorkflowInstance?> GetByIdAsync(GetWorkflowInstanceByIdRequest request, CallContext context = default) => _elsaClient.WorkflowInstances.GetByIdAsync(request.WorkflowInstanceId, context.CancellationToken);
        public Task DeleteAsync(DeleteWorkflowInstanceRequest request, CallContext context = default) => _elsaClient.WorkflowInstances.DeleteAsync(request.WorkflowInstanceId, context.CancellationToken);
    }
}