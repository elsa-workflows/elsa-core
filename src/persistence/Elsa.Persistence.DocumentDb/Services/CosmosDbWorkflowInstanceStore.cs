using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Services;
using Microsoft.Azure.Documents.Client;

namespace Elsa.Persistence.DocumentDb.Services
{
    public class CosmosDbWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly IMapper mapper;
        private readonly IDocumentDbStorage storage;

        public CosmosDbWorkflowInstanceStore(IDocumentDbStorage storage, IMapper mapper)
        {
            this.storage = storage;
            this.mapper = mapper;
        }

        private async Task<DocumentClient> GetDocumentClient() => await storage.GetDocumentClient();
        private Uri GetCollectionUri() => storage.GetWorkflowInstanceCollectionUri();

        public async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            await client.DeleteDocumentAsync(id, cancellationToken: cancellationToken);
        }

        public async Task<WorkflowInstance> GetByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(GetCollectionUri())
                .Where(c => c.CorrelationId == correlationId);
            var document = query.AsEnumerable().FirstOrDefault();
            return Map(document);
        }

        public async Task<WorkflowInstance> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(GetCollectionUri())
                .Where(c => c.Id == id);
            var document = query.AsEnumerable().FirstOrDefault();
            return Map(document);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(GetCollectionUri())
                .OrderByDescending(x => x.CreatedAt);
            return mapper.Map<IEnumerable<WorkflowInstance>>(query);
        }

        public async Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = null,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();

            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(GetCollectionUri())
                .Where(x => x.Status == WorkflowStatus.Executing);

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                query = query.Where(x => x.CorrelationId == correlationId);
            }

            query = query.Where(x => x.BlockingActivities.Any(y => y.ActivityType == activityType));
            query = query.OrderByDescending(x => x.CreatedAt);

            var instances = Map(query.ToList());
            return instances.GetBlockingActivities(activityType);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            string definitionId,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(GetCollectionUri())
                .Where(c => c.DefinitionId == definitionId)
                .OrderByDescending(x => x.CreatedAt);
            return Map(query.ToList());
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            string definitionId,
            WorkflowStatus status,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(GetCollectionUri())
                .Where(c => c.DefinitionId == definitionId && c.Status == status)
                .OrderByDescending(x => x.CreatedAt);
            return Map(query.ToList());
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            WorkflowStatus status,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(GetCollectionUri())
                .Where(c => c.Status == status)
                .OrderByDescending(x => x.CreatedAt);
            return Map(query.ToList());
        }

        public async Task<WorkflowInstance> SaveAsync(
            WorkflowInstance instance,
            CancellationToken cancellationToken = default)
        {
            var document = Map(instance);
            var client = await GetDocumentClient();
            var response = await client.UpsertDocumentWithRetriesAsync(
                GetCollectionUri(),
                document,
                cancellationToken: cancellationToken);

            document = (dynamic)response.Resource;
            return Map(document);
        }

        private WorkflowInstanceDocument Map(WorkflowInstance source) => mapper.Map<WorkflowInstanceDocument>(source);
        private WorkflowInstance Map(WorkflowInstanceDocument source) => mapper.Map<WorkflowInstance>(source);

        private IEnumerable<WorkflowInstance> Map(IEnumerable<WorkflowInstanceDocument> source) =>
            mapper.Map<IEnumerable<WorkflowInstance>>(source);
    }
}