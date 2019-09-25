using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.AutoMapper.Extensions.NodaTime;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Persistence.DocumentDb.Mapping;

namespace Elsa.Persistence.DocumentDb.Services
{
    public class CosmosDbWorkflowInstanceStore : IWorkflowInstanceStore
    {
        readonly IMapper _mapper;
        readonly DocumentDbStorage _storage;

        public CosmosDbWorkflowInstanceStore(DocumentDbStorage storage)
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<InstantProfile>();
                cfg.AddProfile<DocumentProfile>();
            });
            _mapper = configuration.CreateMapper();
            _storage = storage;            
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(_storage.CollectionUri)
                .Where(c => c.Id == id);
            await client.DeleteDocumentAsync(id);
        }

        public Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(_storage.CollectionUri)
                .Where(c => c.CorrelationId == correlationId);
            var document = query.AsEnumerable().FirstOrDefault();
            return Task.FromResult(Map(document));
        }

        public Task<WorkflowInstance> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(_storage.CollectionUri)
                .Where(c => c.Id == id);
            var document = query.AsEnumerable().FirstOrDefault();
            return Task.FromResult(Map(document));
        }

        public Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(_storage.CollectionUri);
            return Task.FromResult(_mapper.Map<IEnumerable<WorkflowInstance>>(query));
        }

        public Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(string activityType, string correlationId = null, CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(_storage.CollectionUri).Where(x => x.Status == WorkflowStatus.Executing);
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                query = query.Where(x => x.CorrelationId == correlationId);
            }
            query = query.Where(x => x.BlockingActivities.Any(y => y.ActivityType == activityType));
            var instances = Map(query.ToList());
            return Task.FromResult(instances.GetBlockingActivities(activityType));
        }

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(_storage.CollectionUri)
                .Where(c => c.DefinitionId == definitionId);
            return Task.FromResult(Map(query.ToList()));
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(string definitionId, WorkflowStatus status, CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(_storage.CollectionUri)
                .Where(c => c.DefinitionId == definitionId && c.Status == status);
            return Task.FromResult(Map(query.ToList()));
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default)
        {
            var client = _storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(_storage.CollectionUri)
                .Where(c => c.Status == status);
            return Task.FromResult(Map(query.ToList()));
        }

        public async Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
        {
            var document = Map(instance);
            var client = _storage.Client;            
            await client.UpsertDocumentWithRetriesAsync(_storage.CollectionUri, document);           
        }

        private WorkflowInstanceDocument Map(WorkflowInstance source) => _mapper.Map<WorkflowInstanceDocument>(source);
        private WorkflowInstance Map(WorkflowInstanceDocument source) => _mapper.Map<WorkflowInstance>(source);

        private IEnumerable<WorkflowInstance> Map(IEnumerable<WorkflowInstanceDocument> source) =>
            _mapper.Map<IEnumerable<WorkflowInstance>>(source);
    }
}
