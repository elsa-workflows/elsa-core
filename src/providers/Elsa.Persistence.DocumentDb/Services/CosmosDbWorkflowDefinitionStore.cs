using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Persistence.DocumentDb.Services
{
    public class CosmosDbWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IMapper mapper;
        private readonly DocumentDbStorage storage;
        private Uri? collectionUrl;
        public CosmosDbWorkflowDefinitionStore(DocumentDbStorage storage, IMapper mapper)
        {
            this.storage = storage;
            this.mapper = mapper;
            collectionUrl = default;
        }
        public async Task<WorkflowDefinition> SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            await client.UpsertDocumentWithRetriesAsync(collectionUrl, document, cancellationToken: cancellationToken);
            return definition;
        }
        public async Task<WorkflowDefinition> AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            await client.CreateDocumentWithRetriesAsync(collectionUrl, document, cancellationToken: cancellationToken);
            return Map(document);
        }
        public async Task<WorkflowDefinition> GetByIdAsync(string tenantId, string id, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            var query = client.CreateDocumentQuery<WorkflowDefinitionDocument>(collectionUrl).Where(c => c.TenantId == tenantId && c.Id == id);
            var document = query.FirstOrDefault();
            return Map(document);
        }

        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            var query = client.CreateDocumentQuery<WorkflowDefinitionDocument>(collectionUrl).Where(x => x.TenantId == tenantId).ToList();

            return mapper.Map<IEnumerable<WorkflowDefinition>>(query);
        }
        public async Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            await client.UpsertDocumentWithRetriesAsync(collectionUrl, document, cancellationToken: cancellationToken);
            return Map(document);
        }

        public async Task<int> DeleteAsync(string tenantId, string id, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            var workflowDefinitionDocuments = await client.CreateDocumentQuery<WorkflowDefinitionDocument>(collectionUrl).Where(c => c.TenantId == tenantId && c.Id == id).ToQueryResultAsync();

            foreach (var record in workflowDefinitionDocuments)
            {
                await client.DeleteDocumentAsync(record.Id, cancellationToken: cancellationToken);
            }
            return workflowDefinitionDocuments.Count;
        }
        private async Task<Uri> GetCollectionUriAsync(CancellationToken cancellationToken)
        {
            if (collectionUrl == null)
                collectionUrl = await storage.GetCollectionAsync("WorkflowDefinitions", cancellationToken);

            return collectionUrl;
        }

        private WorkflowDefinitionDocument Map(WorkflowDefinition source)
        {
            return mapper.Map<WorkflowDefinitionDocument>(source);
        }

        private WorkflowDefinition Map(WorkflowDefinitionDocument source)
        {
            return mapper.Map<WorkflowDefinition>(source);
        }

    }
}
