using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Secrets.Events;
using Elsa.Secrets.Models;
using MediatR;
using Open.Linq.AsyncExtensions;

namespace Elsa.Secrets.Persistence.Decorators
{
    public class EventPublishingSecretsStore : ISecretsStore
    {
        private readonly ISecretsStore _store;
        private readonly IMediator _mediator;

        public EventPublishingSecretsStore(ISecretsStore store, IMediator mediator)
        {
            _store = store;
            _mediator = mediator;
        }

        public Task<int> CountAsync(ISpecification<Secret> specification, CancellationToken cancellationToken = default) => _store.CountAsync(specification, cancellationToken);

        public async Task DeleteAsync(Secret entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new SecretDeleting(entity), cancellationToken);
            await _store.DeleteAsync(entity, cancellationToken);
            await _mediator.Publish(new SecretDeleted(entity), cancellationToken);
        }

        public async Task<int> DeleteManyAsync(ISpecification<Secret> specification, CancellationToken cancellationToken = default)
        {
            var secrets = await FindManyAsync(specification, cancellationToken: cancellationToken).ToList();

            if (!secrets.Any())
                return 0;

            foreach (var secret in secrets)
                await _mediator.Publish(new SecretDeleting(secret), cancellationToken);

            await _mediator.Publish(new ManySecretsDeleting(secrets), cancellationToken);

            var count = await _store.DeleteManyAsync(specification, cancellationToken);

            foreach (var instance in secrets)
                await _mediator.Publish(new SecretDeleted(instance), cancellationToken);

            await _mediator.Publish(new ManySecretsDeleted(secrets), cancellationToken);

            return count;
        }

        public Task<Secret?> FindAsync(ISpecification<Secret> specification, CancellationToken cancellationToken = default) => _store.FindAsync(specification, cancellationToken);

        public Task<IEnumerable<Secret>> FindManyAsync(ISpecification<Secret> specification, IOrderBy<Secret>? orderBy = null, IPaging? paging = null, CancellationToken cancellationToken = default)
            => _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public async Task SaveAsync(Secret entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new SecretSaving(entity), cancellationToken);
            await _store.SaveAsync(entity, cancellationToken);
            await _mediator.Publish(new SecretSaved(entity), cancellationToken);
        }

        public async Task AddAsync(Secret entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new SecretSaving(entity), cancellationToken);
            await _store.AddAsync(entity, cancellationToken);
            await _mediator.Publish(new SecretSaved(entity), cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<Secret> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();

            foreach (var entity in list)
                await _mediator.Publish(new SecretSaving(entity), cancellationToken);

            await _store.AddManyAsync(list, cancellationToken);

            foreach (var entity in list)
                await _mediator.Publish(new SecretSaved(entity), cancellationToken);
        }

        public async Task UpdateAsync(Secret entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new SecretSaving(entity), cancellationToken);
            await _store.UpdateAsync(entity, cancellationToken);
            await _mediator.Publish(new SecretSaved(entity), cancellationToken);
        }
    }
}
