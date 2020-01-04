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
        private readonly IDictionary<string, ProcessInstance> workflowInstances =
            new ConcurrentDictionary<string, ProcessInstance>();

        public Task<ProcessInstance> SaveAsync(ProcessInstance instance, CancellationToken cancellationToken)
        {
            workflowInstances[instance.Id] = instance;
            return Task.FromResult(instance);
        }

        public Task<ProcessInstance> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            var instance = workflowInstances.ContainsKey(id) ? workflowInstances[id] : default;
            return Task.FromResult(instance);
        }

        public Task<ProcessInstance> GetByCorrelationIdAsync(string correlationId,
            CancellationToken cancellationToken = default)
        {
            var instance = workflowInstances.Values.FirstOrDefault(x => x.CorrelationId == correlationId);
            return Task.FromResult(instance);
        }

        public Task<IEnumerable<ProcessInstance>> ListByDefinitionAsync(string definitionId,
            CancellationToken cancellationToken)
        {
            var workflows = workflowInstances.Values.Where(x => x.DefinitionId == definitionId);
            return Task.FromResult(workflows);
        }

        public Task<IEnumerable<ProcessInstance>> ListAllAsync(CancellationToken cancellationToken)
        {
            var workflows = workflowInstances.Values.AsEnumerable();
            return Task.FromResult(workflows);
        }

        public Task<IEnumerable<(ProcessInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = default, CancellationToken cancellationToken = default)
        {
            var query = workflowInstances.Values.AsQueryable();

            query = query.Where(x => x.Status == WorkflowStatus.Suspended);

            if (!string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(x => x.CorrelationId == correlationId);

            query = query.Where(
                x => x.BlockingActivities.Any(y => y.ActivityType == activityType)
            );

            return Task.FromResult(query.AsEnumerable().GetBlockingActivities());
        }

        public Task<IEnumerable<ProcessInstance>> ListByStatusAsync(
            string definitionId,
            WorkflowStatus status,
            CancellationToken cancellationToken)
        {
            var query = workflowInstances.Values.Where(x => x.DefinitionId == definitionId && x.Status == status);
            return Task.FromResult(query);
        }

        public Task<IEnumerable<ProcessInstance>> ListByStatusAsync(WorkflowStatus status,
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