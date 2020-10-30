using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Indexes;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Extensions
{
    public static class WorkflowInstanceManagerExtensions
    {
        public static async Task<WorkflowInstance?> GetByIdAsync(
            this IWorkflowInstanceManager manager,
            string workflowInstanceId,
            CancellationToken cancellationToken = default) =>
            await manager.Query<WorkflowInstanceIndex>(x => x.WorkflowInstanceId == workflowInstanceId).FirstOrDefaultAsync();
        
        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(
            this IWorkflowInstanceManager manager,
            string workflowDefinitionId,
            WorkflowStatus workflowStatus,
            CancellationToken cancellationToken = default) =>
            manager
                .Query<WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId && x.WorkflowStatus == workflowStatus)
                .ListAsync();

        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            this IWorkflowInstanceManager manager,
            string workflowDefinitionId,
            CancellationToken cancellationToken = default) =>
            manager.Query<WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId).ListAsync();
    }
}