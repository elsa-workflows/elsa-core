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

        public static async Task<WorkflowInstance?> GetByCorrelationIdAsync(
            this IWorkflowInstanceManager manager,
            string correlationId,
            WorkflowStatus status,
            CancellationToken cancellationToken = default) =>
            await manager.Query<WorkflowInstanceIndex>(x => x.CorrelationId == correlationId && x.WorkflowStatus == status).FirstOrDefaultAsync();

        public static async Task<int> CountAsync(this IWorkflowInstanceManager manager, CancellationToken cancellationToken = default) => await manager.Query<WorkflowInstanceIndex>().CountAsync();

        public static async Task<IEnumerable<WorkflowInstance>> ListAsync(
            this IWorkflowInstanceManager manager,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            await manager.Query<WorkflowInstanceIndex>().Skip(page * pageSize).Take(pageSize).ListAsync();

        public static async Task<IEnumerable<WorkflowInstance>> ListByCorrelationIdAsync(
            this IWorkflowInstanceManager manager,
            string correlationId,
            WorkflowStatus status,
            CancellationToken cancellationToken = default) =>
            await manager.Query<WorkflowInstanceIndex>(x => x.CorrelationId == correlationId && x.WorkflowStatus == status).ListAsync();

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