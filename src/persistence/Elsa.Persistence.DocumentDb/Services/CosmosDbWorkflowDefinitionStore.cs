using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Extensions;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Services;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Elsa.Persistence.DocumentDb.Services
{
    public class CosmosDbWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IMapper mapper;
        private readonly IDocumentDbStorage storage;

        public CosmosDbWorkflowDefinitionStore(IDocumentDbStorage storage, IMapper mapper)
        {
            this.storage = storage;
            this.mapper = mapper;
        }

        private async Task<DocumentClient> GetDocumentClient() => await storage.GetDocumentClient();
        private async Task<(string Name, Uri Uri, string TenantId)> GetCollectionInfoAsync() => 
            await storage.GetWorkflowDefinitionCollectionInfoAsync();

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var requestOptions = new RequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            document.TenantId = tenantId;
            var response = await client.CreateDocumentWithRetriesAsync(uri, document, requestOptions, cancellationToken: cancellationToken);
            document = (dynamic)response.Resource;

            return Map(document);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
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
                .CreateDocumentQuery<WorkflowDefinitionVersionDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .Where(c => c.DefinitionId == id);
            var documents = await query.ToQueryResultAsync(cancellationToken);
            var tasks = documents.Select(d =>
            {
                var documentUri = new Uri(d.SelfLink, UriKind.Relative);
                return client.DeleteDocumentWithRetriesAsync(documentUri,
                    requestOptions,
                    cancellationToken);
            }).ToArray();

            await Task.WhenAll(tasks);

            return documents.Count;
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client
                .CreateDocumentQuery<WorkflowDefinitionVersionDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .Where(c => c.DefinitionId == id)
                .WithVersion(version);
            var document = query.AsEnumerable().FirstOrDefault();

            return Map(document);
        }
        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client
                .CreateDocumentQuery<WorkflowDefinitionVersionDocument>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId)
                .WithVersion(version);
            var documents = await query.ToQueryResultAsync(cancellationToken);

            return Map(documents);
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = await GetDocumentClient();
            var (_, uri, tenantId) = await GetCollectionInfoAsync();
            var requestOptions = new RequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            document.TenantId = tenantId;
            var response = await client.UpsertDocumentWithRetriesAsync(uri, document, requestOptions, cancellationToken: cancellationToken);

            document = (dynamic)response.Resource;
            return Map(document);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            return await SaveAsync(definition, cancellationToken);
        }

        private WorkflowDefinitionVersionDocument Map(WorkflowDefinitionVersion source) => mapper.Map<WorkflowDefinitionVersionDocument>(source);
        private WorkflowDefinitionVersion Map(WorkflowDefinitionVersionDocument source) => mapper.Map<WorkflowDefinitionVersion>(source);
        private IEnumerable<WorkflowDefinitionVersion> Map(IEnumerable<WorkflowDefinitionVersionDocument> source) => 
            mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(source);
    }
}
