using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;

namespace Elsa.Persistence.Decorators
{
    public class SuspendingWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly IWorkflowInstanceStore _store;
        private readonly ISuspendedWorkflowStore _suspendedWorkflowStore;

        public SuspendingWorkflowInstanceStore(IWorkflowInstanceStore store, ISuspendedWorkflowStore suspendedWorkflowStore)
        {
            _store = store;
            _suspendedWorkflowStore = suspendedWorkflowStore;
        }
        
        public async Task SaveAsync(WorkflowInstance entity, CancellationToken cancellationToken)
        {
            // Save entity.
            await _store.SaveAsync(entity, cancellationToken);
            
            // Delete blocking activity records.
            await DeleteBlockingActivitiesAsync(entity.EntityId, cancellationToken);
            
            if (entity.WorkflowStatus == WorkflowStatus.Suspended)
            {
                // Create blocking activity records.
                foreach (var blockingActivity in entity.BlockingActivities)
                {
                    await _suspendedWorkflowStore.SaveAsync(new SuspendedWorkflowBlockingActivity
                    {
                        EntityId = $"{entity.EntityId}-{blockingActivity.ActivityId}",
                        Version = entity.Version,
                        ActivityId = blockingActivity.ActivityId,
                        ActivityType = blockingActivity.ActivityType,
                        ContextId = entity.ContextId,
                        CorrelationId = entity.CorrelationId,
                        CreatedAt = entity.CreatedAt,
                        DefinitionId = entity.DefinitionId,
                        InstanceId = entity.EntityId,
                        TenantId = entity.TenantId,
                        LastExecutedAt = entity.LastExecutedAt
                    }, cancellationToken);
                }
            }
        }

        public async Task DeleteAsync(WorkflowInstance entity, CancellationToken cancellationToken)
        {
            await DeleteBlockingActivitiesAsync(entity.EntityId, cancellationToken);
            await _store.DeleteAsync(entity, cancellationToken);
        }

        public async Task<int> DeleteManyAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken)
        {
            var workflowInstances = await FindManyAsync(specification, null, null, cancellationToken);

            foreach (var workflowInstance in workflowInstances) 
                await DeleteBlockingActivitiesAsync(workflowInstance.EntityId, cancellationToken);

            return await _store.DeleteManyAsync(specification, cancellationToken);
        }

        public Task<IEnumerable<WorkflowInstance>> FindManyAsync(ISpecification<WorkflowInstance> specification, IOrderBy<WorkflowInstance>? orderBy, IPaging? paging, CancellationToken cancellationToken) =>
            _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public Task<int> CountAsync(ISpecification<WorkflowInstance> specification, IOrderBy<WorkflowInstance>? orderBy, CancellationToken cancellationToken) => _store.CountAsync(specification, orderBy, cancellationToken);
        public Task<WorkflowInstance?> FindAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken) => _store.FindAsync(specification, cancellationToken);
        
        private async Task DeleteBlockingActivitiesAsync(string workflowInstanceId, CancellationToken cancellationToken) => 
            await _suspendedWorkflowStore.DeleteManyAsync(new SuspendedWorkflowInstanceIdSpecification(workflowInstanceId), cancellationToken);
    }
}