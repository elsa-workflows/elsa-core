using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Services.Models;
using ProcessInstance = Elsa.Models.ProcessInstance;

namespace Elsa.Persistence.DocumentDb.Services
{
    public class CosmosDbWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly IMapper mapper;
        private readonly DocumentDbStorage storage;

        public CosmosDbWorkflowInstanceStore(DocumentDbStorage storage, IMapper mapper)
        {
            this.storage = storage;
            this.mapper = mapper;
        }

        public async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            await client.DeleteDocumentAsync(id, cancellationToken: cancellationToken);
        }

        public Task<ProcessInstance> GetByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(c => c.CorrelationId == correlationId);
            var document = query.AsEnumerable().FirstOrDefault();
            return Task.FromResult(Map(document));
        }

        public Task<ProcessInstance> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(c => c.Id == id);
            var document = query.AsEnumerable().FirstOrDefault();
            return Task.FromResult(Map(document));
        }

        public Task<IEnumerable<ProcessInstance>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .OrderByDescending(x => x.CreatedAt);
            return Task.FromResult(mapper.Map<IEnumerable<ProcessInstance>>(query));
        }

        public Task<IEnumerable<(Elsa.Services.Models.ProcessInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = null,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;

            var query = client
                .CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(x => x.Status == ProcessStatus.Running);

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                query = query.Where(x => x.CorrelationId == correlationId);
            }

            query = query.Where(x => x.BlockingActivities.Any(y => y.ActivityType == activityType));
            query = query.OrderByDescending(x => x.CreatedAt);

            var instances = Map(query.ToList());
            return Task.FromResult(instances.GetBlockingActivities(activityType));
        }

        public Task<IEnumerable<ProcessInstance>> ListByDefinitionAsync(
            string definitionId,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(c => c.DefinitionId == definitionId)
                .OrderByDescending(x => x.CreatedAt);
            return Task.FromResult(Map(query.ToList()));
        }

        public Task<IEnumerable<ProcessInstance>> ListByStatusAsync(
            string definitionId,
            ProcessStatus status,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(c => c.DefinitionId == definitionId && c.Status == status)
                .OrderByDescending(x => x.CreatedAt);
            return Task.FromResult(Map(query.ToList()));
        }

        public Task<IEnumerable<ProcessInstance>> ListByStatusAsync(
            ProcessStatus status,
            CancellationToken cancellationToken = default)
        {
            var client = storage.Client;
            var query = client.CreateDocumentQuery<WorkflowInstanceDocument>(storage.CollectionUri)
                .Where(c => c.Status == status)
                .OrderByDescending(x => x.CreatedAt);
            return Task.FromResult(Map(query.ToList()));
        }

        public async Task<ProcessInstance> SaveAsync(
            ProcessInstance instance,
            CancellationToken cancellationToken = default)
        {
            var document = Map(instance);
            var client = storage.Client;
            var response = await client.UpsertDocumentWithRetriesAsync(
                storage.CollectionUri,
                document,
                cancellationToken: cancellationToken);

            document = (dynamic)response.Resource;
            return Map(document);
        }

        private WorkflowInstanceDocument Map(ProcessInstance source) => mapper.Map<WorkflowInstanceDocument>(source);
        private ProcessInstance Map(WorkflowInstanceDocument source) => mapper.Map<ProcessInstance>(source);

        private IEnumerable<ProcessInstance> Map(IEnumerable<WorkflowInstanceDocument> source) =>
            mapper.Map<IEnumerable<ProcessInstance>>(source);
    }
}