using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
            string? tenantId,
            WorkflowStatus workflowStatus,
            CancellationToken cancellationToken = default)
        {
            // See: https://github.com/sebastienros/yessql/issues/298
            Expression<Func<WorkflowInstanceIndex, bool>> query = tenantId == null
                ? x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == null && x.WorkflowStatus == workflowStatus
                : x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId && x.WorkflowStatus == workflowStatus;
                
            return manager.Query(query).ListAsync();
        }

        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(
            this IWorkflowInstanceManager manager,
            string workflowDefinitionId,
            WorkflowStatus workflowStatus,
            CancellationToken cancellationToken = default) =>
            manager.ListByDefinitionAndStatusAsync(workflowDefinitionId, default, workflowStatus, cancellationToken);

        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            this IWorkflowInstanceManager manager,
            string workflowDefinitionId,
            string? tenantId,
            CancellationToken cancellationToken = default)
        {
            // See: https://github.com/sebastienros/yessql/issues/298
            Expression<Func<WorkflowInstanceIndex, bool>> query = tenantId == null 
                ? x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == null 
                : x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId;
            
            return manager.Query(query).ListAsync();
        }

        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            this IWorkflowInstanceManager manager,
            string workflowDefinitionId,
            CancellationToken cancellationToken = default) =>
            manager.ListByDefinitionAsync(workflowDefinitionId, default, cancellationToken);
    }
}