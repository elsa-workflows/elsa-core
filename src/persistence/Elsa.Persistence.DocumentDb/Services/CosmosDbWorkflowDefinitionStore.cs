using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.AutoMapper.Extensions.NodaTime;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Extensions;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Persistence.DocumentDb.Mapping;

namespace Elsa.Persistence.DocumentDb.Services
{
    public class CosmosDbWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        readonly IMapper _mapper;
        readonly DocumentDbStorage _storage;

        public CosmosDbWorkflowDefinitionStore(DocumentDbStorage storage)
        {
            _storage = storage;
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<InstantProfile>();
                cfg.AddProfile<DocumentProfile>();
            });
            _mapper = configuration.CreateMapper();
        }

        public async Task AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = _storage.Client;
            await client.CreateDocumentWithRetriesAsync(_storage.CollectionUri, document);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var records = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(_storage.CollectionUri).Where(c => c.DefinitionId == id).ToQueryResult();
            foreach (var record in records)
            {
                await client.DeleteDocumentAsync(record.Id);
            }
            return records.Count;
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(_storage.CollectionUri)
                .Where(c => c.DefinitionId == id).WithVersion(version);
            var document = query.AsEnumerable().FirstOrDefault();
            return Task.FromResult(Map(document));
        }

        public Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var query = client.CreateDocumentQuery<WorkflowDefinitionVersionDocument>(_storage.CollectionUri)
                .WithVersion(version).ToList();
           
            return Task.FromResult(_mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(query));
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = _storage.Client;
            await client.UpsertDocumentWithRetriesAsync(_storage.CollectionUri, document);
            return definition;
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            var client = _storage.Client;
            await client.UpsertDocumentWithRetriesAsync(_storage.CollectionUri, document);
            return Map(document);
        }

        private WorkflowDefinitionVersionDocument Map(WorkflowDefinitionVersion source)
        {
            return _mapper.Map<WorkflowDefinitionVersionDocument>(source);
        }

        private WorkflowDefinitionVersion Map(WorkflowDefinitionVersionDocument source)
        {
            return _mapper.Map<WorkflowDefinitionVersion>(source);
        }
    }
}
