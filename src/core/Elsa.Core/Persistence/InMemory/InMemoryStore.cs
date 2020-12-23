using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryStore<T> : IStore<T> where T : IEntity
    {
        protected readonly Dictionary<string, T> Entities = new();

        public InMemoryStore(IIdGenerator idGenerator)
        {
            IdGenerator = idGenerator;
        }

        private IIdGenerator IdGenerator { get; }
        
        public Task SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity.Id == null!) 
                entity.Id = IdGenerator.Generate();

            Entities[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            if(Entities.ContainsKey(entity.Id))
                Entities.Remove(entity.Id);
            
            return Task.CompletedTask;
        }

        public async Task<int> DeleteManyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var entities = await FindManyAsync(specification, cancellationToken: cancellationToken).ToList();

            foreach (var entity in entities) 
                Entities.Remove(entity.Id);

            return entities.Count;
        }

        public Task<IEnumerable<T>> FindManyAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            var query = Entities.Values.AsQueryable().Apply(specification).Apply(orderBy).Apply(paging);
            return Task.FromResult<IEnumerable<T>>(query.ToList());
        }

        public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var query = Entities.Values.AsQueryable().Apply(specification);
            return Task.FromResult(query.Count());
        }

        public Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var result = Entities.Values.AsQueryable().FirstOrDefault(specification.ToExpression());
            return Task.FromResult(result)!;
        }
    }
}