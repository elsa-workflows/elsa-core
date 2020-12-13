using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Specifications;

namespace Elsa.Persistence
{
    public interface IWorkflowInstanceStore
    {
        Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        Task DeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);

        Task<IEnumerable<WorkflowInstance>> ListAsync(
            ISpecification<WorkflowInstance> specification, IGroupingSpecification<WorkflowInstance>? grouping = default, 
            IPagingSpecification? paging = default,
            CancellationToken cancellationToken = default);
        
        Task<int> CountAsync(ISpecification<WorkflowInstance> specification, IGroupingSpecification<WorkflowInstance>? grouping = default, CancellationToken cancellationToken = default);
        Task<WorkflowInstance?> FindAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken = default);

        Task<WorkflowInstance?> GetByIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByBlockingActivityTypeAsync(string activityType, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(string workflowDefinitionId, string? tenantId, WorkflowStatus workflowStatus, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus workflowStatus, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string workflowDefinitionId, string? tenantId, CancellationToken cancellationToken = default);
        Task<WorkflowInstance?> GetByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);
    }
}