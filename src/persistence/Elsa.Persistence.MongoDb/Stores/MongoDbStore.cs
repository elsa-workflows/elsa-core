using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbStore<T> : IStore<T> where T : class, IEntity
    {
        public MongoDbStore(IMongoCollection<T> collection, IIdGenerator idGenerator)
        {
            Collection = collection;
            IdGenerator = idGenerator;
        }

        protected IIdGenerator IdGenerator { get; }
        protected IMongoCollection<T> Collection { get; }

        public async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity.Id == null!)
                entity.Id = IdGenerator.Generate();

            var filter = GetFilter(entity.Id);
            await Collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }
        
        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity.Id == null!)
                entity.Id = IdGenerator.Generate();

            var filter = GetFilter(entity.Id);
            await Collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = false }, cancellationToken);
        }

        public Task AddAsync(T entity, CancellationToken cancellationToken = default) => SaveAsync(entity, cancellationToken);

        public async Task AddManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();

            foreach (var entity in list.Where(x => x.Id == null!))
                entity.Id = IdGenerator.Generate();

            if (!list.Any())
                return;
            
            await Collection.InsertManyAsync(list, default, cancellationToken);
        }

        public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            var filter = GetFilter(entity.Id);
            await Collection.DeleteOneAsync(filter, cancellationToken);
        }

        public async Task<int> DeleteManyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var filter = Builders<T>.Filter.Where(specification.ToExpression());
            var result = await Collection.DeleteManyAsync(filter, cancellationToken);
            return (int)result.DeletedCount;
        }

        public async Task<IEnumerable<T>> FindManyAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default) =>
            await Collection.AsQueryable().Apply(specification).Apply(orderBy).Apply(paging).ToListAsync(cancellationToken);

        public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
            await Collection.AsQueryable().Apply(specification).CountAsync(cancellationToken);

        public async Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) => await Collection.AsQueryable().Where(specification.ToExpression()).FirstOrDefaultAsync(cancellationToken);

        protected FilterDefinition<T> GetFilter(string id) => Builders<T>.Filter.Where(x => x.Id == id);
    }
}