using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Extensions;
using Elsa.Persistence.DocumentDb.Helpers;

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

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            await client.CreateDocumentWithRetriesAsync(collectionUrl, document, cancellationToken: cancellationToken);
            return Map(document);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(collectionUrl).Where(c => c.Id == id);
            var document = query.FirstOrDefault();
            return Map(document);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(string definitionId, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(collectionUrl)
                .Where(c => c.DefinitionId == definitionId).WithVersion(version);
            var document = query.AsEnumerable().FirstOrDefault();
            return Map(document);
        }
        
        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            var workflowDefinitionDocuments = await client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(collectionUrl).Where(c => c.DefinitionId == id).ToQueryResultAsync();
            foreach (var record in workflowDefinitionDocuments)
            {
                await client.DeleteDocumentAsync(record.Id, cancellationToken: cancellationToken);
            }
            return workflowDefinitionDocuments.Count;
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            var query = client
                .CreateDocumentQuery<WorkflowDefinitionVersionDocument>(collectionUrl)
                .WithVersion(version).ToList();
           
            return mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(query);
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            await client.UpsertDocumentWithRetriesAsync(collectionUrl, document, cancellationToken: cancellationToken);
            return definition;
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            await client.UpsertDocumentWithRetriesAsync(collectionUrl, document, cancellationToken: cancellationToken);
            return Map(document);
        }
        
        private async Task<Uri> GetCollectionUriAsync(CancellationToken cancellationToken)
        {
            if (collectionUrl == null) 
                collectionUrl = await storage.GetCollectionAsync("WorkflowDefinitions", cancellationToken);

            return collectionUrl;
        }

        private WorkflowDefinitionVersionDocument Map(WorkflowDefinitionVersion source)
        {
            return mapper.Map<WorkflowDefinitionVersionDocument>(source);
        }

        private WorkflowDefinitionVersion Map(WorkflowDefinitionVersionDocument source)
        {
            return mapper.Map<WorkflowDefinitionVersion>(source);
        }
    }
}
