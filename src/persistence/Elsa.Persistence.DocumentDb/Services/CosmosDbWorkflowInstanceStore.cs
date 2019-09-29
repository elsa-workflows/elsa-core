using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.AutoMapper.Extensions.NodaTime;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Persistence.DocumentDb.Mapping;

namespace Elsa.Persistence.DocumentDb.Services
{
    public class CosmosDbWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly IMapper mapper;
        private readonly DocumentDbStorage storage;

        public CosmosDbWorkflowInstanceStore(DocumentDbStorage storage)
        {
            var configuration = new MapperConfiguration(
                cfg =>
                {
                    cfg.AddProfile<InstantProfile>();
                    cfg.AddProfile<DocumentProfile>();
                });
            mapper = configuration.CreateMapper();
            this.storage = storage;
        }

        public async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            await client.DeleteDocumentAsync(id, cancellationToken: cancellationToken);
        }

        public Task<WorkflowInstance> GetByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(c => c.CorrelationId == correlationId);
            var document = query.AsEnumerable().FirstOrDefault();
            return Task.FromResult(Map(document));
        }

        public Task<WorkflowInstance> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(c => c.Id == id);
            var document = query.AsEnumerable().FirstOrDefault();
            return Task.FromResult(Map(document));
        }

        public Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .OrderByDescending(x => x.CreatedAt);
            return Task.FromResult(mapper.Map<IEnumerable<WorkflowInstance>>(query));
        }

        public Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = null,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;

            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(x => x.Status == WorkflowStatus.Executing);

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                query = query.Where(x => x.CorrelationId == correlationId);
            }

            query = query.Where(x => x.BlockingActivities.Any(y => y.ActivityType == activityType));
            query = query.OrderByDescending(x => x.CreatedAt);

            var instances = Map(query.ToList());
            return Task.FromResult(instances.GetBlockingActivities(activityType));
        }

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            string definitionId,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(c => c.DefinitionId == definitionId)
                .OrderByDescending(x => x.CreatedAt);
            return Task.FromResult(Map(query.ToList()));
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            string definitionId,
            WorkflowStatus status,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(c => c.DefinitionId == definitionId && c.Status == status)
                .OrderByDescending(x => x.CreatedAt);
            return Task.FromResult(Map(query.ToList()));
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            WorkflowStatus status,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(c => c.Status == status)
                .OrderByDescending(x => x.CreatedAt);
            return Task.FromResult(Map(query.ToList()));
        }

        public async Task SaveAsync(
            WorkflowInstance instance,
            CancellationToken cancellationToken = default)
        {
            var document = Map(instance);
            var client = storage.Client;
            await client.UpsertDocumentWithRetriesAsync(
                storage.CollectionUri,
                document,
                cancellationToken: cancellationToken);
        }

        private WorkflowInstanceDocument Map(WorkflowInstance source) => mapper.Map<WorkflowInstanceDocument>(source);
        private WorkflowInstance Map(WorkflowInstanceDocument source) => mapper.Map<WorkflowInstance>(source);

        private IEnumerable<WorkflowInstance> Map(IEnumerable<WorkflowInstanceDocument> source) =>
            mapper.Map<IEnumerable<WorkflowInstance>>(source);
    }
}