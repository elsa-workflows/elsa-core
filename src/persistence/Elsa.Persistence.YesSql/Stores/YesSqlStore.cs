using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using YesSql;
using YesSql.Indexes;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public abstract class YesSqlStore<T, TDocument> : IStore<T> where T : class, IEntity where TDocument : class
    {
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly Stopwatch _stopwatch = new();

        public YesSqlStore(ISession session, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlStore<T, TDocument>> logger, string collectionName)
        {
            _logger = logger;
            IdGenerator = idGenerator;
            Mapper = mapper;
            CollectionName = collectionName;
            Session = session;
        }

        protected ISession Session { get; }
        protected IIdGenerator IdGenerator { get; }
        protected IMapper Mapper { get; }
        protected string CollectionName { get; }

        public virtual async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                var existingDocument = await FindDocumentAsync(entity, cancellationToken);
                var document = Mapper.Map(entity, existingDocument);
                Session.Save(document, CollectionName);
                await Session.CommitAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default) => await SaveAsync(entity, cancellationToken);

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            var document = Mapper.Map<TDocument>(entity);
            Session.Save(document, CollectionName);
            await Session.CommitAsync();
        }

        public async Task AddManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var documents = entities.Select(x => Mapper.Map<TDocument>(x));

            foreach (var document in documents) 
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
            _stopwatch.Restart();
            var documents = await Query(specification).ListAsync().ToList();
            _stopwatch.Stop();
            _logger.LogDebug("Loading documents to delete took {TimeElapsed}", _stopwatch.Elapsed);

            foreach (var document in documents)
            {
                _stopwatch.Restart();
                Session.Delete(document, CollectionName);
                _stopwatch.Stop();
                _logger.LogDebug("Deleting document took {TimeElapsed}", _stopwatch.Elapsed);
            }

            _stopwatch.Restart();
            await Session.CommitAsync();
            _stopwatch.Stop();
            _logger.LogDebug("Committing deleted documents took {TimeElapsed}", _stopwatch.Elapsed);
            
            return documents.Count;
        }

        public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            _stopwatch.Restart();
            var count = await Query(specification).CountAsync();
            _stopwatch.Stop();
            _logger.LogDebug("CountAsync took {TimeElapsed}", _stopwatch.Elapsed);
            return count;
        }

        public async Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var document = await Query(specification).FirstOrDefaultAsync()!;
            return Map(document);
        }

        public virtual async Task<IEnumerable<T>> FindManyAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            _stopwatch.Restart();
            var documents = await Query(specification, orderBy, paging, cancellationToken).ListAsync();
            var mappedDocuments = Map(documents);
            _stopwatch.Stop();
            _logger.LogDebug("FindManyAsync took {TimeElapsed}", _stopwatch.Elapsed);
            return mappedDocuments;
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
            var expression = specification.ToExpression().ConvertType<T, TDocument>().ConvertType<TDocument, TIndex>();
            return Query<TIndex>(expression);
        }

        protected TDocument Map(T source) => Mapper.Map<TDocument>(source);
        protected T Map(TDocument source) => Mapper.Map<T>(source);
        protected IEnumerable<T> Map(IEnumerable<TDocument> source) => Mapper.Map<IEnumerable<T>>(source);

        protected IQuery<TDocument> Query() => Session.Query<TDocument>(CollectionName);
        protected IQuery<TDocument, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex => Session.Query<TDocument, TIndex>(predicate, CollectionName);
        protected IQuery<TDocument, TIndex> Query<TIndex>() where TIndex : class, IIndex => Session.Query<TDocument, TIndex>(CollectionName);
    }
}