using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Persistence;

namespace Elsa.Activities.Webhooks.Persistence.Decorators
{
    public class InitializingWebhookDefinitionStore : IWebhookDefinitionStore
    {
        private readonly IWebhookDefinitionStore _store;
        private readonly IIdGenerator _idGenerator;

        public InitializingWebhookDefinitionStore(IWebhookDefinitionStore store, IIdGenerator idGenerator)
        {
            _store = store;
            _idGenerator = idGenerator;
        }

        public async Task SaveAsync(WebhookDefinition entity, CancellationToken cancellationToken)
        {
            entity = Initialize(entity);
            await _store.SaveAsync(entity, cancellationToken);
        }

        public async Task UpdateAsync(WebhookDefinition entity, CancellationToken cancellationToken)
        {
            entity = Initialize(entity);
            await _store.UpdateAsync(entity, cancellationToken);
        }

        public async Task AddAsync(WebhookDefinition entity, CancellationToken cancellationToken = default)
        {
            entity = Initialize(entity);
            await _store.AddAsync(entity, cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<WebhookDefinition> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();

            foreach (var entity in list)
                Initialize(entity);

            await _store.AddManyAsync(list, cancellationToken);
        }

        public Task DeleteAsync(WebhookDefinition entity, CancellationToken cancellationToken) => _store.DeleteAsync(entity, cancellationToken);
        public Task<int> DeleteManyAsync(ISpecification<WebhookDefinition> specification, CancellationToken cancellationToken) => _store.DeleteManyAsync(specification, cancellationToken);

        public Task<IEnumerable<WebhookDefinition>> FindManyAsync(
            ISpecification<WebhookDefinition> specification,
            IOrderBy<WebhookDefinition>? orderBy,
            IPaging? paging,
            CancellationToken cancellationToken) =>
            _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public Task<int> CountAsync(ISpecification<WebhookDefinition> specification, CancellationToken cancellationToken) => _store.CountAsync(specification, cancellationToken);

        public Task<WebhookDefinition?> FindAsync(ISpecification<WebhookDefinition> specification, CancellationToken cancellationToken) => _store.FindAsync(specification, cancellationToken);

        private WebhookDefinition Initialize(WebhookDefinition webhookDefinition)
        {
            if (string.IsNullOrWhiteSpace(webhookDefinition.Id))
                webhookDefinition.Id = _idGenerator.Generate();

            return webhookDefinition;
        }
    }
}