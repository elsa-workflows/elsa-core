using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Services;

namespace Elsa.Persistence.DocumentDb.Services
{
    public class CosmosDbWorkflowInstanceStore : CosmosDbWorkflowStoreBase<WorkflowInstance, WorkflowInstanceDocument>, IWorkflowInstanceStore
    {
        public CosmosDbWorkflowInstanceStore(IMapper mapper, ICosmosDbStoreHelper<WorkflowInstanceDocument> cosmosDbStoreHelper) 
            : base(mapper, cosmosDbStoreHelper) { }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var document = await cosmosDbStoreHelper.FirstOrDefaultAsync(q => q.Where(c => c.Id == id));
            if (document != null)
            {
                await cosmosDbStoreHelper.DeleteAsync(document, cancellationToken);
            }
        }

        public async Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
        {
            var document = await cosmosDbStoreHelper.FirstOrDefaultAsync(q => q.Where(c => c.CorrelationId == correlationId));
            return Map(document);
        }

        public async Task<WorkflowInstance> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var document = await cosmosDbStoreHelper.FirstOrDefaultAsync(q => q.Where(c => c.Id == id));
            return Map(document);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            var documents = await cosmosDbStoreHelper.ListAsync(q => q.OrderByDescending(x => x.CreatedAt), cancellationToken);
            return Map(documents);
        }

        public async Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(string activityType, string correlationId = null,
            CancellationToken cancellationToken = default)
        {
            var documents = await cosmosDbStoreHelper.ListAsync(q =>
            {
                var query = q.Where(x => x.Status == WorkflowStatus.Executing);
                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    query = query.Where(x => x.CorrelationId == correlationId);
                }

                query = query.Where(x => x.BlockingActivities.Any(y => y.ActivityType == activityType));
                return query.OrderByDescending(x => x.CreatedAt);
            }, cancellationToken);

            var instances = Map(documents);
            return instances.GetBlockingActivities(activityType).ToArray();
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var documents = await cosmosDbStoreHelper.ListAsync(q =>
            {
                return q.Where(c => c.DefinitionId == definitionId)
                    .OrderByDescending(x => x.CreatedAt);
            }, cancellationToken);

            return Map(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(string definitionId, WorkflowStatus status,
            CancellationToken cancellationToken = default)
        {
            var documents = await cosmosDbStoreHelper.ListAsync(q =>
            {
                return q.Where(c => c.DefinitionId == definitionId && c.Status == status)
                    .OrderByDescending(x => x.CreatedAt);
            }, cancellationToken);

            return Map(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default)
        {
            var documents = await cosmosDbStoreHelper.ListAsync(q =>
            {
                return q.Where(c => c.Status == status)
                    .OrderByDescending(x => x.CreatedAt);
            }, cancellationToken);

            return Map(documents);
        }

        public async Task<WorkflowInstance> SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
        {
            var document = Map(instance);
            document = await cosmosDbStoreHelper.SaveAsync(document, cancellationToken);
            return Map(document);
        }
    }
}