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
    public abstract class YesSqlStore<T, TDocument> : IStore<T> where T : class, IEntity where TDocument : class
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
            var documents = await Query(specification).ListAsync().ToList();

            foreach (var document in documents)
                Session.Delete(document, CollectionName);

            await Session.CommitAsync();
            return documents.Count;
        }

        public async Task<int> CountAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, CancellationToken cancellationToken = default) => await Query(specification, orderBy).CountAsync();

        public async Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var document = await Query(specification).FirstOrDefaultAsync()!;
            return Map(document);
        }

        public virtual async Task<IEnumerable<T>> FindManyAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            var documents = await Query(specification, orderBy, paging, cancellationToken).ListAsync();
            return Map(documents);
        }
        
        protected virtual IQuery<TDocument> Query(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            var query = ToQuery(specification);

            if (orderBy != null) 
                query = OrderBy(query, orderBy, specification);
            
            if (paging != null)
                query = query.Skip(paging.Skip).Take(paging.Take);

            return query;
        }

        protected abstract Task<TDocument?> FindDocumentAsync(T entity, CancellationToken cancellationToken);

        protected virtual IQuery<TDocument> OrderBy(IQuery<TDocument> query, IOrderBy<T> orderBy, ISpecification<T> specification) => query;

        protected virtual IQuery<TDocument> ToQuery(ISpecification<T> specification)
        {
            return MapSpecification(specification);
        }

        protected abstract IQuery<TDocument> MapSpecification(ISpecification<T> specification);

        protected IQuery<TDocument> AutoMapSpecification<TIndex>(ISpecification<T> specification) where TIndex : class, IIndex
        {
            var expression = specification.ToExpression().ConvertType<T, TIndex>();
            return Query<TIndex>(expression);
        }

        protected TDocument Map(T source) => Mapper.Map<TDocument>(source);
        protected T Map(TDocument source) => Mapper.Map<T>(source);
        protected IEnumerable<T> Map(IEnumerable<TDocument> source) => Mapper.Map<IEnumerable<T>>(source);

        protected IQuery<TDocument> Query() => Session.Query<TDocument>(CollectionName);
        protected IQuery<TDocument, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex => Session.Query<TDocument, TIndex>(predicate, CollectionName);
    }
}