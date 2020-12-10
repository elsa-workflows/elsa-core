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
using Microsoft.Azure.Documents;
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
        private async Task<(string Name, Uri Uri, string TenantId)> GetCollectionInfoAsync() =>
            await storage.GetWorkflowInstanceCollectionInfoAsync();

        public async Task DeleteAsync(string id,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var requestOptions = new RequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .Where(c => c.Id == id);
            var document = query.AsEnumerable().FirstOrDefault();
            if (document != null)
            {
                var documentUri = new Uri(document.SelfLink, UriKind.Relative);
                await client.DeleteDocumentWithRetriesAsync(documentUri,
                    requestOptions, 
                    cancellationToken);
            }
        }

        public async Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .Where(c => c.CorrelationId == correlationId);
            var document = query.AsEnumerable().FirstOrDefault();

            return Map(document);
        }

        public async Task<WorkflowInstance> GetByIdAsync(string id,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .Where(c => c.Id == id);
            var document = query.AsEnumerable().FirstOrDefault();

            return Map(document);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .OrderByDescending(x => x.CreatedAt);
            var documents = await query.ToQueryResultAsync(cancellationToken);

            return Map(documents);
        }

        public async Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = null,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .Where(x => x.Status == WorkflowStatus.Executing);

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                query = query.Where(x => x.CorrelationId == correlationId);
            }

            query = query.Where(x => x.BlockingActivities.Any(y => y.ActivityType == activityType));
            query = query.OrderByDescending(x => x.CreatedAt);

            var documents = await query.ToQueryResultAsync(cancellationToken);

            var instances = Map(documents);
            return instances.GetBlockingActivities(activityType).ToArray();
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            string definitionId,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .Where(c => c.DefinitionId == definitionId)
                .OrderByDescending(x => x.CreatedAt);
            var documents = await query.ToQueryResultAsync(cancellationToken);

            return Map(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            string definitionId,
            WorkflowStatus status,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .Where(c => c.DefinitionId == definitionId && c.Status == status)
                .OrderByDescending(x => x.CreatedAt);
            var documents = await query.ToQueryResultAsync(cancellationToken);

            return Map(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            WorkflowStatus status,
            CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .Where(c => c.Status == status)
                .OrderByDescending(x => x.CreatedAt);
            var documents = await query.ToQueryResultAsync(cancellationToken);

            return Map(documents);
        }

        public async Task<WorkflowInstance> SaveAsync(
            WorkflowInstance instance,
            CancellationToken cancellationToken = default)
        {
            var document = Map(instance);
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var requestOptions = new RequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            document.TenantId = tenantId;
            var response = await client.UpsertDocumentWithRetriesAsync(
                uri,
                document,
                requestOptions,
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