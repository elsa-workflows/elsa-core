using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Client.Models;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    public static class WorkflowInstanceServiceExtensions
    {
        public static Task<PagedList<WorkflowInstanceSummary>> ListAsync(
            this IWorkflowInstanceService service,
            int page = 0,
            int pageSize = 50,
            string? workflowDefinitionId = default,
            WorkflowStatus? workflowStatus = default,
            OrderBy? orderBy = default,
            string? searchTerm = default) =>
            service.ListAsync(new ListWorkflowInstancesRequest(page, pageSize, workflowDefinitionId, workflowStatus, orderBy, searchTerm));

        public static Task<WorkflowInstance?> GetByIdAsync(this IWorkflowInstanceService service, string workflowInstanceId) => service.GetByIdAsync(new GetWorkflowInstanceByIdRequest(workflowInstanceId));
        public static Task DeleteAsync(this IWorkflowInstanceService service, string workflowInstanceId) => service.DeleteAsync(new DeleteWorkflowRequest(workflowInstanceId));
        public static Task BulkDeleteAsync(this IWorkflowInstanceService service, IEnumerable<string> workflowInstanceIds) => service.BulkDeleteAsync(new BulkDeleteWorkflowInstancesRequest(workflowInstanceIds));
        public static Task RetryAsync(this IWorkflowInstanceService service, string workflowInstanceId) => service.RetryAsync(new RetryWorkflowRequest(workflowInstanceId));
        public static Task BulkRetryAsync(this IWorkflowInstanceService service, IEnumerable<string> workflowInstanceIds) => service.BulkRetryAsync(new BulkRetryWorkflowInstancesRequest(workflowInstanceIds));
    }
}