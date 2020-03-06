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
    public class CosmosDbWorkflowDefinitionVersionStore : IWorkflowDefinitionVersionStore
    {
        private readonly IMapper mapper;
        private readonly DocumentDbStorage storage;
        private Uri? collectionUrl;

        public CosmosDbWorkflowDefinitionVersionStore(DocumentDbStorage storage, IMapper mapper)
        {
            this.storage = storage;
            this.mapper = mapper;
            collectionUrl = default;
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken = default)
        {
            var document = Map(definitionVersion);
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
            var workflowDefinitionVersionDocuments = await client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(collectionUrl).Where(c => c.DefinitionId == id).ToQueryResultAsync();
            foreach (var record in workflowDefinitionVersionDocuments)
            {
                await client.DeleteDocumentAsync(record.Id, cancellationToken: cancellationToken);
            }
            return workflowDefinitionVersionDocuments.Count;
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

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken = default)
        {
            var document = Map(definitionVersion);
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            await client.UpsertDocumentWithRetriesAsync(collectionUrl, document, cancellationToken: cancellationToken);
            return definitionVersion;
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken = default)
        {
            var document = Map(definitionVersion);
            var client = storage.Client;
            var collectionUrl = await GetCollectionUriAsync(cancellationToken);
            await client.UpsertDocumentWithRetriesAsync(collectionUrl, document, cancellationToken: cancellationToken);
            return Map(document);
        }

        private async Task<Uri> GetCollectionUriAsync(CancellationToken cancellationToken)
        {
            if (collectionUrl == null)
                collectionUrl = await storage.GetCollectionAsync("WorkflowDefinitionVersions", cancellationToken);

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
