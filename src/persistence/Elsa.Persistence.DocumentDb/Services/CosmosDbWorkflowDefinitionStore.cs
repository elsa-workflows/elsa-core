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

        public CosmosDbWorkflowDefinitionStore(DocumentDbStorage storage, IMapper mapper)
        {
            this.storage = storage;
            this.mapper = mapper;
        }

        public async Task<ProcessDefinitionVersion> AddAsync(ProcessDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            await client.CreateDocumentWithRetriesAsync(storage.CollectionUri, document, cancellationToken: cancellationToken);
            return Map(document);
        }

        public Task<ProcessDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(storage.CollectionUri).Where(c => c.Id == id);
            var document = query.FirstOrDefault();
            return Task.FromResult(Map(document));
        }

        public Task<ProcessDefinitionVersion> GetByIdAsync(string definitionId, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(storage.CollectionUri)
                .Where(c => c.DefinitionId == definitionId).WithVersion(version);
            var document = query.AsEnumerable().FirstOrDefault();
            return Task.FromResult(Map(document));
        }
        
        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var workflowDefinitionDocuments = await client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(storage.CollectionUri).Where(c => c.DefinitionId == id).ToQueryResultAsync();
            foreach (var record in workflowDefinitionDocuments)
            {
                await client.DeleteDocumentAsync(record.Id, cancellationToken: cancellationToken);
            }
            return workflowDefinitionDocuments.Count;
        }

        public Task<IEnumerable<ProcessDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(storage.CollectionUri)
                .WithVersion(version).ToList();
           
            return Task.FromResult(mapper.Map<IEnumerable<ProcessDefinitionVersion>>(query));
        }

        public async Task<ProcessDefinitionVersion> SaveAsync(ProcessDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            await client.UpsertDocumentWithRetriesAsync(storage.CollectionUri, document, cancellationToken: cancellationToken);
            return definition;
        }

        public async Task<ProcessDefinitionVersion> UpdateAsync(ProcessDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            await client.UpsertDocumentWithRetriesAsync(storage.CollectionUri, document, cancellationToken: cancellationToken);
            return Map(document);
        }

        private WorkflowDefinitionVersionDocument Map(ProcessDefinitionVersion source)
        {
            return mapper.Map<WorkflowDefinitionVersionDocument>(source);
        }

        private ProcessDefinitionVersion Map(WorkflowDefinitionVersionDocument source)
        {
            return mapper.Map<ProcessDefinitionVersion>(source);
        }
    }
}
