using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Events;
using Elsa.Models;
using Elsa.Repositories;

using MediatR;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb
{
    public class MongoDbWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly IMongoCollection<WorkflowInstance> _workflowInstances;
        private readonly IMediator _mediator;

        public MongoDbWorkflowInstanceStore(IMediator mediator, IMongoCollection<WorkflowInstance> workflowInstances)
        {
            _mediator = mediator;
            _workflowInstances = workflowInstances;
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        { 
            var count = await _workflowInstances.CountDocumentsAsync(new BsonDocument());

            return (int)count;
        }

        public async Task DeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            var filter = GetFilterWorkflowId(workflowInstance.Id);
            await _workflowInstances.DeleteOneAsync(filter);
            await _mediator.Publish(new WorkflowInstanceDeleted(workflowInstance), cancellationToken);
        }

        public Task<WorkflowInstance?> GetByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default)
        {
           return _workflowInstances.AsQueryable()
                        .FirstOrDefaultAsync(instance => instance.CorrelationId == correlationId && instance.Status == status);
        }

        public async Task<WorkflowInstance> GetByIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {

            return await _workflowInstances
                .AsQueryable().FirstOrDefaultAsync(instance => instance.WorkflowInstanceId == workflowInstanceId);
        }

        public Task<bool> IsWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId, CancellationToken cancellationToken = default)
        {
            return _workflowInstances
                    .AsQueryable().AnyAsync(instance => instance.WorkflowDefinitionId == workflowDefinitionId
                    && (instance.Status == WorkflowStatus.Running || instance.Status == WorkflowStatus.Suspended)
                    && instance.TenantId == tenantId);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _workflowInstances
                    .AsQueryable().Skip(page * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByBlockingActivityTypeAsync(string activityType, CancellationToken cancellationToken = default)
        {
            return await _workflowInstances
                .AsQueryable().Where(instance => instance.BlockingActivities.Any(a => a.ActivityType == activityType)).ToListAsync();
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default)
        {
            return await _workflowInstances
                .AsQueryable()
                .Where(instance => instance.CorrelationId == correlationId && instance.Status == status).ToListAsync();
        }

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(string workflowDefinitionId, WorkflowStatus workflowStatus, CancellationToken cancellationToken = default)
        {
            return ListByDefinitionAndStatusAsync(workflowDefinitionId, default, workflowStatus, cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(string workflowDefinitionId, string? tenantId, WorkflowStatus workflowStatus, CancellationToken cancellationToken = default)
        {
            return await _workflowInstances
                .AsQueryable()
                .Where(instance => instance.WorkflowDefinitionId == workflowDefinitionId && instance.Status == workflowStatus
                    && instance.TenantId == tenantId).ToListAsync();
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string workflowDefinitionId, string? tenantId, CancellationToken cancellationToken = default)
        {
            return await _workflowInstances
                .AsQueryable().Where(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId).ToListAsync();
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus workflowStatus, CancellationToken cancellationToken = default)
        {
            return await _workflowInstances
                .AsQueryable()
                .Where(instance => instance.Status == workflowStatus).ToListAsync();
        }

        public async Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {  
            if(workflowInstance.Id == 0)
            {
                // If there is no instance yet, max throws an error
                if (await _workflowInstances.AsQueryable().AnyAsync())
                {
                    workflowInstance.Id = await _workflowInstances.AsQueryable().MaxAsync(x => x.Id) + 1;
                } else
                {
                    workflowInstance.Id = 1;
                }              
            }

            var filter = GetFilterWorkflowId(workflowInstance.Id);

            await _workflowInstances.ReplaceOneAsync(filter, workflowInstance, new ReplaceOptions { IsUpsert = true });
            await _mediator.Publish(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
        }

        private FilterDefinition<WorkflowInstance> GetFilterWorkflowId(int id) => Builders<WorkflowInstance>.Filter.Where(x => x.Id == id);
    }
}
