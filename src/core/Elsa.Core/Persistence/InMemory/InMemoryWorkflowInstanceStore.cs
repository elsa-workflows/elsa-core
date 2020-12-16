using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowInstanceStore : InMemoryStore<WorkflowInstance>, IWorkflowInstanceStore
    {
        public InMemoryWorkflowInstanceStore(IIdGenerator idGenerator) : base(idGenerator)
        {
        }
        
        public Task<WorkflowInstance?> GetByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entities.Values.FirstOrDefault(instance => instance.CorrelationId == correlationId && instance.WorkflowStatus == status))!;

        public Task<WorkflowInstance?> GetByIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entities.Values
                .FirstOrDefault(instance => instance.EntityId == workflowInstanceId))!;

        public Task<IEnumerable<WorkflowInstance>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(Entities.Values.Skip(page * pageSize).Take(pageSize));

        public Task<IEnumerable<WorkflowInstance>> ListByBlockingActivityTypeAsync(string activityType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entities.Values
                .Where(instance => instance.BlockingActivities.Any(a => a.ActivityType == activityType)));

        public Task<IEnumerable<WorkflowInstance>> ListByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entities.Values.Where(instance => instance.CorrelationId == correlationId && instance.WorkflowStatus == status));

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(string workflowDefinitionId, string tenantId, WorkflowStatus workflowStatus, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entities.Values
                .Where(instance => instance.DefinitionId == workflowDefinitionId && instance.WorkflowStatus == workflowStatus && instance.TenantId == tenantId));

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string workflowDefinitionId, string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entities.Values.Where(x => x.DefinitionId == workflowDefinitionId && x.TenantId == tenantId));

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus workflowStatus, CancellationToken cancellationToken = default) => Task.FromResult(Entities.Values.Where(instance => instance.WorkflowStatus == workflowStatus));
    }
}