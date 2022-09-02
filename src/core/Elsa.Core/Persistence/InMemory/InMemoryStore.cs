using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryStore<T> : IStore<T> where T : IEntity
    {
        protected readonly IMemoryCache MemoryCache;

        public InMemoryStore(IMemoryCache memoryCache, IIdGenerator idGenerator)
        {
            MemoryCache = memoryCache;
            IdGenerator = idGenerator;
        }

        private IIdGenerator IdGenerator { get; }
        private string CacheKey => GetType().Name;

        public async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity.Id == null!) 
                entity.Id = IdGenerator.Generate();

            var dictionary = await GetDictionaryAsync();
            dictionary[entity.Id] = entity;
            SetDictionary(dictionary);
        }
        
        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            var dictionary = await GetDictionaryAsync();
            dictionary[entity.Id] = entity;
            SetDictionary(dictionary);
        }

        public Task AddAsync(T entity, CancellationToken cancellationToken = default) => SaveAsync(entity, cancellationToken);

        public async Task AddManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var dictionary = await GetDictionaryAsync();
            
            foreach (var entity in entities) 
                dictionary[entity.Id] = entity;
            
            SetDictionary(dictionary);
        }

        public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            var dictionary = await GetDictionaryAsync();

            if (dictionary.ContainsKey(entity.Id))
            {
                dictionary.Remove(entity.Id);
                SetDictionary(dictionary);
            }
        }

        public async Task<int> DeleteManyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var entities = await FindManyAsync(specification, cancellationToken: cancellationToken).ToList();
            var dictionary = await GetDictionaryAsync();

            foreach (var entity in entities) 
                dictionary.Remove(entity.Id);

            SetDictionary(dictionary);
            return entities.Count;
        }

        public async Task<IEnumerable<T>> FindManyAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            var dictionary = await GetDictionaryAsync();
            var query = dictionary.Values.AsQueryable().Apply(specification).Apply(orderBy).Apply(paging);
            return query.ToList();
        }

        public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var dictionary = await GetDictionaryAsync();
            var query = dictionary.Values.AsQueryable().Apply(specification);
            return query.Count();
        }

        public async Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var dictionary = await GetDictionaryAsync();
            return dictionary.Values.AsQueryable().FirstOrDefault(specification.ToExpression());
        }

        public async Task<IEnumerable<TOut>> FindManyAsync<TOut>(ISpecification<T> specification
            , System.Linq.Expressions.Expression<System.Func<T, TOut>> funcMapping
            , IOrderBy<T>? orderBy = null, IPaging? paging = null, CancellationToken cancellationToken = default) where TOut : class
        {
            var dictionary = await GetDictionaryAsync();
            var query = dictionary.Values.AsQueryable().Apply(specification).Apply(orderBy).Apply(paging);
            return query.Select(funcMapping).ToList();
        }
        private async Task<IDictionary<string, T>> GetDictionaryAsync() => await MemoryCache.GetOrCreateAsync(CacheKey, _ => Task.FromResult(new ConcurrentDictionary<string, T>()));
        private void SetDictionary(IDictionary<string, T> dictionary) => MemoryCache.Set(CacheKey, dictionary);
    }
}