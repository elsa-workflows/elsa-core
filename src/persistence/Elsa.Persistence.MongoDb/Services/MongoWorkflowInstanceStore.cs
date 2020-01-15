using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb.Services
{
    public class MongoWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly IMongoCollection<WorkflowInstance> collection;

        public MongoWorkflowInstanceStore(IMongoCollection<WorkflowInstance> collection)
        {
            this.collection = collection;
        }

        public async Task<WorkflowInstance> SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken)
        {
            await collection.ReplaceOneAsync(
                x => x.Id == instance.Id,
                instance,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);

            return instance;
        }

        public async Task<WorkflowInstance> GetByIdAsync(
            string id,
            CancellationToken cancellationToken)
        {
            return await collection.AsQueryable().Where(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<WorkflowInstance> GetByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            return await collection.AsQueryable()
                .Where(x => x.CorrelationId == correlationId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            string definitionId,
            CancellationToken cancellationToken)
        {
            return await collection.AsQueryable()
                .Where(x => x.DefinitionId == definitionId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken)
        {
            return await collection
                .AsQueryable()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var query = collection.AsQueryable();

            query = query.Where(x => x.Status == WorkflowStatus.Executing);

            if (!string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(x => x.CorrelationId == correlationId);

            query = query.Where(x => x.BlockingActivities.Any(y => y.ActivityType == activityType));
            query = query.OrderByDescending(x => x.CreatedAt);
            
            var instances = await query.ToListAsync(cancellationToken);

            return instances.GetBlockingActivities(activityType);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            string definitionId,
            WorkflowStatus status,
            CancellationToken cancellationToken)
        {
            return await collection
                .AsQueryable()
                .Where(x => x.DefinitionId == definitionId && x.Status == status)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            WorkflowStatus status,
            CancellationToken cancellationToken)
        {
            return await collection
                .AsQueryable()
                .Where(x => x.Status == status)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            await collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
        }
    }
}