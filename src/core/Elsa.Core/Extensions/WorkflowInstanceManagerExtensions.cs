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
            return manager.Query<WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId && x.WorkflowStatus == workflowStatus).ListAsync();
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
            return manager.Query<WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId).ListAsync();
        }

        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            this IWorkflowInstanceManager manager,
            string workflowDefinitionId,
            CancellationToken cancellationToken = default) =>
            manager.ListByDefinitionAsync(workflowDefinitionId, default, cancellationToken);
    }
}