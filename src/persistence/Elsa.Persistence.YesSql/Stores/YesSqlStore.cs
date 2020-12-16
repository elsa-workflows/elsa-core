using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.YesSql.Extensions;
using Open.Linq.AsyncExtensions;
using YesSql;
using YesSql.Indexes;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public abstract class YesSqlStore<T, TDocument, TIndex> : IStore<T> where T : class, IEntity where TIndex: class, IIndex where TDocument : class
    {
        public YesSqlStore(ISession session, IIdGenerator idGenerator, IMapper mapper, string collectionName)
        {
            IdGenerator = idGenerator;
            Mapper = mapper;
            CollectionName = collectionName;
            Session = session;
        }

        protected ISession Session { get; }
        protected IIdGenerator IdGenerator { get; }
        protected IMapper Mapper { get; }
        protected string CollectionName { get; }

        public async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            var existingDocument = await FindDocumentAsync(entity, cancellationToken);
            var document = Mapper.Map(entity, existingDocument);
            Session.Save(document, CollectionName);
            await Session.CommitAsync();
        }

        public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            var document = await FindDocumentAsync(entity, cancellationToken);
            Session.Delete(document, CollectionName);
            await Session.CommitAsync();
        }

        public async Task<int> DeleteManyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var documents = await Query().Apply(specification).ListAsync().ToList();

            foreach (var document in documents) 
                Session.Delete(document, CollectionName);

            await Session.CommitAsync();
            return documents.Count;
        }

        public async Task<IEnumerable<T>> FindManyAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            var documents = await Query().Apply(specification).Apply(orderBy).Apply(paging).ListAsync()!;
            return Map(documents);
        }

        public async Task<int> CountAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, CancellationToken cancellationToken = default) => await Query().Apply(specification).Apply(orderBy).CountAsync();

        public async Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var document = await Query().Apply(specification).FirstOrDefaultAsync()!;
            return Map(document);
        }

        protected abstract Task<TDocument?> FindDocumentAsync(T entity, CancellationToken cancellationToken);
        
        protected TDocument Map(T source) => Mapper.Map<TDocument>(source);
        protected T Map(TDocument source) => Mapper.Map<T>(source);
        protected IEnumerable<T> Map(IEnumerable<TDocument> source) => Mapper.Map<IEnumerable<T>>(source);

        protected IQuery<TDocument, TIndex> Query() => Session.Query<TDocument, TIndex>(CollectionName);
        protected IQuery<TDocument, TIndex> Query(Expression<Func<TIndex, bool>> predicate) => Session.Query<TDocument, TIndex>(predicate, CollectionName);
        
        
    }
}