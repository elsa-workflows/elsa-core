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
        private Uri GetCollectionUri() => storage.GetWorkflowDefinitionCollectionUri();

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = await GetDocumentClient();
            await client.CreateDocumentWithRetriesAsync(GetCollectionUri(), document, cancellationToken: cancellationToken);
            return Map(document);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var records = await client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(GetCollectionUri()).Where(c => c.DefinitionId == id).ToQueryResultAsync();
            foreach (var record in records)
            {
                await client.DeleteDocumentAsync(record.SelfLink, cancellationToken: cancellationToken);
            }
            return records.Count;
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(GetCollectionUri())
                .Where(c => c.DefinitionId == id).WithVersion(version);
            var document = query.AsEnumerable().FirstOrDefault();
            return Map(document);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = await GetDocumentClient();
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(GetCollectionUri())
                .WithVersion(version).ToList();
           
            return mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(query);
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = await GetDocumentClient();
            await client.UpsertDocumentWithRetriesAsync(GetCollectionUri(), document, cancellationToken: cancellationToken);
            return definition;
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = await GetDocumentClient();
            await client.UpsertDocumentWithRetriesAsync(GetCollectionUri(), document, cancellationToken: cancellationToken);
            return Map(document);
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
