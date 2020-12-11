using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using MediatR;

#nullable disable

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private static readonly List<WorkflowInstance> Instances = new List<WorkflowInstance>();
        private readonly IMediator _mediator;

        public InMemoryWorkflowInstanceStore(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Instances.Count());

        public async Task DeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            Instances.Remove(workflowInstance);
            await _mediator.Publish(new WorkflowInstanceDeleted(workflowInstance), cancellationToken);
        }

        public Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances.FirstOrDefault(instance => instance.CorrelationId == correlationId && instance.Status == status));

        public Task<WorkflowInstance> GetByIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances
                .FirstOrDefault(instance => instance.WorkflowInstanceId == workflowInstanceId));

        public Task<bool> GetWorkflowIsAlreadyExecutingAsync(string tenantId, string workflowDefinitionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances
                .Any(instance =>
                    instance.WorkflowDefinitionId == workflowDefinitionId
                    && (instance.Status == WorkflowStatus.Running || instance.Status == WorkflowStatus.Suspended)
                    && instance.TenantId == tenantId));

        public Task<IEnumerable<WorkflowInstance>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(Instances.Skip(page * pageSize).Take(pageSize));

        public Task<IEnumerable<WorkflowInstance>> ListByBlockingActivityTypeAsync(string activityType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances
                .Where(instance => instance.BlockingActivities.Any(a => a.ActivityType == activityType)));

        public Task<IEnumerable<WorkflowInstance>> ListByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances.Where(instance => instance.CorrelationId == correlationId && instance.Status == status));

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(string workflowDefinitionId, string tenantId, WorkflowStatus workflowStatus, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances
                .Where(instance => instance.WorkflowDefinitionId == workflowDefinitionId && instance.Status == workflowStatus && instance.TenantId == tenantId));

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string workflowDefinitionId, string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances.Where(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId));

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus workflowStatus, CancellationToken cancellationToken = default) => Task.FromResult(Instances.Where(instance => instance.Status == workflowStatus));

        public async Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            if (workflowInstance.Id == 0)
            {
                workflowInstance.Id = workflowInstance.WorkflowInstanceId.GetHashCode();
                Instances.Add(workflowInstance);
            }
            else
            {
                for (var i = 0; i < Instances.Count; i++)
                    if (workflowInstance.WorkflowInstanceId == Instances[i].WorkflowInstanceId)
                        Instances[i] = workflowInstance;
            }

            await _mediator.Publish(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
        }
    }
}