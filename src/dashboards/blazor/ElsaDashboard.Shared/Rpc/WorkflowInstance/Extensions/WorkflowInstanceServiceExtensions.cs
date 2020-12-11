using System.Threading.Tasks;
using Elsa.Client.Models;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    public static class WorkflowInstanceServiceExtensions
    {
        public static Task<PagedList<WorkflowInstance>> ListAsync(this IWorkflowInstanceService service, int page = 0, int pageSize = 50) => service.ListAsync(new ListWorkflowInstancesRequest(page, pageSize));
        public static Task<WorkflowInstance?> GetByIdAsync(this IWorkflowInstanceService service, string workflowInstanceId) => service.GetByIdAsync(new GetWorkflowInstanceByIdRequest(workflowInstanceId));
        public static Task DeleteAsync(this IWorkflowInstanceService service, string workflowInstanceId) => service.DeleteAsync(new DeleteWorkflowInstanceRequest(workflowInstanceId));
    }
}