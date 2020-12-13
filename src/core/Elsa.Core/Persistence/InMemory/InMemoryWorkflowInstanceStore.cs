using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Specifications;

#nullable disable

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private static readonly List<WorkflowInstance> Instances = new();

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Instances.Count());

        public Task DeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            Instances.Remove(workflowInstance);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<WorkflowInstance>> ListAsync(ISpecification<WorkflowInstance> specification, IGroupingSpecification<WorkflowInstance>? grouping, IPagingSpecification? paging, CancellationToken cancellationToken)
        {
            var query = Instances.AsQueryable().Apply(specification).Apply(grouping).Apply(paging);
            return Task.FromResult<IEnumerable<WorkflowInstance>>(query.ToList());
        }

        public Task<int> CountAsync(ISpecification<WorkflowInstance> specification, IGroupingSpecification<WorkflowInstance>? grouping = default, CancellationToken cancellationToken = default)
        {
            var query = Instances.AsQueryable().Apply(specification).Apply(grouping);
            return Task.FromResult(query.Count());
        }

        public Task<WorkflowInstance?> FindAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken = default)
        {
            var result = Instances.AsQueryable().FirstOrDefault(specification.ToExpression());
            return Task.FromResult(result);
        }

        public Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances.FirstOrDefault(instance => instance.CorrelationId == correlationId && instance.WorkflowStatus == status));

        public Task<WorkflowInstance> GetByIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances
                .FirstOrDefault(instance => instance.WorkflowInstanceId == workflowInstanceId));

        public Task<IEnumerable<WorkflowInstance>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(Instances.Skip(page * pageSize).Take(pageSize));

        public Task<IEnumerable<WorkflowInstance>> ListByBlockingActivityTypeAsync(string activityType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances
                .Where(instance => instance.BlockingActivities.Any(a => a.ActivityType == activityType)));

        public Task<IEnumerable<WorkflowInstance>> ListByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances.Where(instance => instance.CorrelationId == correlationId && instance.WorkflowStatus == status));

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(string workflowDefinitionId, string tenantId, WorkflowStatus workflowStatus, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances
                .Where(instance => instance.WorkflowDefinitionId == workflowDefinitionId && instance.WorkflowStatus == workflowStatus && instance.TenantId == tenantId));

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string workflowDefinitionId, string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Instances.Where(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId));

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus workflowStatus, CancellationToken cancellationToken = default) => Task.FromResult(Instances.Where(instance => instance.WorkflowStatus == workflowStatus));

        public Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
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

            return Task.CompletedTask;
        }
    }
}