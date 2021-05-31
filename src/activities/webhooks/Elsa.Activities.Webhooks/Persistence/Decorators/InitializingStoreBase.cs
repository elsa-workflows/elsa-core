using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;

namespace Elsa.Activities.Webhooks.Persistence.Decorators
{
    public abstract class InitializingStoreBase<T> : IStore<T>
        where T : IEntity
    {
        private readonly IStore<T> _store;

        protected InitializingStoreBase(IStore<T> store, IIdGenerator idGenerator)
        {
            _store = store;
            IdGenerator = idGenerator;
        }

        protected IIdGenerator IdGenerator { get; }

        public async Task SaveAsync(T entity, CancellationToken cancellationToken)
        {
            entity = Initialize(entity);
            await _store.SaveAsync(entity, cancellationToken);
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken)
        {
            entity = Initialize(entity);
            await _store.UpdateAsync(entity, cancellationToken);
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            entity = Initialize(entity);
            await _store.AddAsync(entity, cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();

            foreach (var entity in list)
                Initialize(entity);

            await _store.AddManyAsync(list, cancellationToken);
        }

        public Task DeleteAsync(T entity, CancellationToken cancellationToken) => _store.DeleteAsync(entity, cancellationToken);
        public Task<int> DeleteManyAsync(ISpecification<T> specification, CancellationToken cancellationToken) => _store.DeleteManyAsync(specification, cancellationToken);

        public Task<IEnumerable<T>> FindManyAsync(
            ISpecification<T> specification,
            IOrderBy<T>? orderBy,
            IPaging? paging,
            CancellationToken cancellationToken) =>
            _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken) => _store.CountAsync(specification, cancellationToken);

        public Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken) => _store.FindAsync(specification, cancellationToken);

        protected abstract T Initialize(T entity);
    }
}