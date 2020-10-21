using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.Memory
{
    public class MemoryWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly IDictionary<string, WorkflowInstance> workflowInstances =
            new ConcurrentDictionary<string, WorkflowInstance>();

        public Task<WorkflowInstance> SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken)
        {
            workflowInstances[instance.Id] = instance;
            return Task.FromResult(instance);
        }

        public Task<WorkflowInstance> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            workflowInstances.TryGetValue(id, out var instance);
            return Task.FromResult(instance);
        }

        public Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId,
            CancellationToken cancellationToken = default)
        {
            var instance = workflowInstances.Values.FirstOrDefault(x => x.CorrelationId == correlationId);
            return Task.FromResult(instance);
        }

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string definitionId,
            CancellationToken cancellationToken)
        {
            var workflows = workflowInstances.Values.Where(x => x.DefinitionId == definitionId);
            return Task.FromResult(workflows);
        }

        public Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken)
        {
            var workflows = workflowInstances.Values.AsEnumerable();
            return Task.FromResult(workflows);
        }

        public Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = default, CancellationToken cancellationToken = default)
        {
            var query = workflowInstances.Values.AsQueryable();

            query = query.Where(x => x.Status == WorkflowStatus.Executing);

            if (!string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(x => x.CorrelationId == correlationId);

            query = query.Where(
                x => x.BlockingActivities.Any(y => y.ActivityType == activityType)
            );

            return Task.FromResult(query.AsEnumerable().GetBlockingActivities());
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            string definitionId,
            WorkflowStatus status,
            CancellationToken cancellationToken)
        {
            var query = workflowInstances.Values.Where(x => x.DefinitionId == definitionId && x.Status == status);
            return Task.FromResult(query);
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status,
            CancellationToken cancellationToken)
        {
            var query = workflowInstances.Values.Where(x => x.Status == status);
            return Task.FromResult(query);
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            workflowInstances.Remove(id);
            return Task.CompletedTask;
        }
    }
}