using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization.Models;

namespace Elsa.Core.Persistence.Memory
{
    public class MemoryWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly IDictionary<string, WorkflowInstance> workflowInstances = new Dictionary<string, WorkflowInstance>();
        
        public Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            workflowInstances[workflowInstance.Id] = workflowInstance;
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
            var query =
                from workflowInstance in workflowInstances.Values
                from blockingActivityId in workflowInstance.BlockingActivities
                let blockingActivity = workflowInstance.Activities[blockingActivityId]
                where blockingActivity.TypeName == activityType
                select (workflowInstance, blockingActivity);

            return Task.FromResult(query.Distinct());
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