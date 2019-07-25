using System;
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
        private readonly IDictionary<string, WorkflowInstance> workflowInstances = new Dictionary<string, WorkflowInstance>();
        
        public Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken)
        {
            workflowInstances[instance.Id] = instance;
            return Task.CompletedTask;
        }

        public Task<WorkflowInstance> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            var instance = workflowInstances.ContainsKey(id) ? workflowInstances[id] : default;
            return Task.FromResult(instance);
        }

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string definitionId, CancellationToken cancellationToken)
        {
            var workflows = workflowInstances.Values.Where(x => x.DefinitionId == definitionId);
            return Task.FromResult(workflows);
        }

        public Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken)
        {
            var workflows = workflowInstances.Values.AsEnumerable();
            return Task.FromResult(workflows);
        }

        public Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(string activityType, CancellationToken cancellationToken)
        {
            var query = workflowInstances.Values.GetBlockingActivities().Where(x => x.Item2.TypeName == activityType);

            return Task.FromResult(query);
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(string definitionId, WorkflowStatus status, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}