using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Webhooks.Events;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Persistence;
using MediatR;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Webhooks.Persistence.Decorators
{
    public class EventPublishingWebhookDefinitionStore : IWebhookDefinitionStore
    {
        private readonly IWebhookDefinitionStore _store;
        private readonly IMediator _mediator;

        public EventPublishingWebhookDefinitionStore(IWebhookDefinitionStore store, IMediator mediator)
        {
            _store = store;
            _mediator = mediator;
        }

        public Task<int> CountAsync(ISpecification<WebhookDefinition> specification, CancellationToken cancellationToken = default) => _store.CountAsync(specification, cancellationToken);

        public async Task DeleteAsync(WebhookDefinition entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WebhookDefinitionDeleting(entity), cancellationToken);
            await _store.DeleteAsync(entity, cancellationToken);
            await _mediator.Publish(new WebhookDefinitionDeleted(entity), cancellationToken);
        }

        public async Task<int> DeleteManyAsync(ISpecification<WebhookDefinition> specification, CancellationToken cancellationToken = default)
        {
            var webhookDefinitions = await FindManyAsync(specification, cancellationToken: cancellationToken).ToList();

            if (!webhookDefinitions.Any())
                return 0;

            foreach (var webhookDefinition in webhookDefinitions)
                await _mediator.Publish(new WebhookDefinitionDeleting(webhookDefinition), cancellationToken);

            await _mediator.Publish(new ManyWebhookDefinitionsDeleting(webhookDefinitions), cancellationToken);

            var count = await _store.DeleteManyAsync(specification, cancellationToken);

            foreach (var instance in webhookDefinitions)
                await _mediator.Publish(new WebhookDefinitionDeleted(instance), cancellationToken);

            await _mediator.Publish(new ManyWebhookDefinitionsDeleted(webhookDefinitions), cancellationToken);

            return count;
        }

        public Task<WebhookDefinition?> FindAsync(ISpecification<WebhookDefinition> specification, CancellationToken cancellationToken = default) => _store.FindAsync(specification, cancellationToken);

        public Task<IEnumerable<WebhookDefinition>> FindManyAsync(ISpecification<WebhookDefinition> specification, IOrderBy<WebhookDefinition>? orderBy = null, IPaging? paging = null, CancellationToken cancellationToken = default)
            => _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public async Task SaveAsync(WebhookDefinition entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WebhookDefinitionSaving(entity), cancellationToken);
            await _store.SaveAsync(entity, cancellationToken);
            await _mediator.Publish(new WebhookDefinitionSaved(entity), cancellationToken);
        }

        public async Task AddAsync(WebhookDefinition entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WebhookDefinitionSaving(entity), cancellationToken);
            await _store.AddAsync(entity, cancellationToken);
            await _mediator.Publish(new WebhookDefinitionSaved(entity), cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<WebhookDefinition> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();

            foreach (var entity in list)
                await _mediator.Publish(new WebhookDefinitionSaving(entity), cancellationToken);

            await _store.AddManyAsync(list, cancellationToken);

            foreach (var entity in list)
                await _mediator.Publish(new WebhookDefinitionSaved(entity), cancellationToken);
        }

        public async Task UpdateAsync(WebhookDefinition entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WebhookDefinitionSaving(entity), cancellationToken);
            await _store.UpdateAsync(entity, cancellationToken);
            await _mediator.Publish(new WebhookDefinitionSaved(entity), cancellationToken);
        }
    }
}