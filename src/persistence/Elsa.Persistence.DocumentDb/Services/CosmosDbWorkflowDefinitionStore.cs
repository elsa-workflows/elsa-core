using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.AutoMapper.Extensions.NodaTime;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Extensions;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Persistence.DocumentDb.Mapping;

namespace Elsa.Persistence.DocumentDb.Services
{
    public class CosmosDbWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IMapper mapper;
        private readonly DocumentDbStorage storage;

        public CosmosDbWorkflowDefinitionStore(DocumentDbStorage storage)
        {
            this.storage = storage;
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<InstantProfile>();
                cfg.AddProfile<DocumentProfile>();
            });
            mapper = configuration.CreateMapper();
        }

        public async Task AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            await client.CreateDocumentWithRetriesAsync(storage.CollectionUri, document, cancellationToken: cancellationToken);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var records = await client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(storage.CollectionUri).Where(c => c.DefinitionId == id).ToQueryResultAsync();
            foreach (var record in records)
            {
                await client.DeleteDocumentAsync(record.Id, cancellationToken: cancellationToken);
            }
            return records.Count;
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(storage.CollectionUri)
                .Where(c => c.DefinitionId == id).WithVersion(version);
            var document = query.AsEnumerable().FirstOrDefault();
            return Task.FromResult(Map(document));
        }

        public Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(storage.CollectionUri)
                .WithVersion(version).ToList();
           
            return Task.FromResult(mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(query));
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            await client.UpsertDocumentWithRetriesAsync(storage.CollectionUri, document, cancellationToken: cancellationToken);
            return definition;
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = storage.Client;
            await client.UpsertDocumentWithRetriesAsync(storage.CollectionUri, document, cancellationToken: cancellationToken);
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
