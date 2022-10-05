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
using Elsa.Persistence.YesSql.Services;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using YesSql;
using YesSql.Indexes;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public abstract class YesSqlStore<T, TDocument> : IStore<T> where T : class, IEntity where TDocument : class
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly Stopwatch _stopwatch = new();

        public YesSqlStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlStore<T, TDocument>> logger, string collectionName)
        {
            SessionProvider = sessionProvider;
            Logger = logger;
            IdGenerator = idGenerator;
            Mapper = mapper;
            CollectionName = collectionName;
        }
        
        protected ISessionProvider SessionProvider { get; }
        protected IIdGenerator IdGenerator { get; }
        protected IMapper Mapper { get; }
        protected ILogger Logger { get; }
        protected string CollectionName { get; }

        public virtual async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                await using var session = SessionProvider.CreateSession();
                var existingDocument = await FindDocumentAsync(session, entity, cancellationToken);
                var document = Mapper.Map(entity, existingDocument);
                session.Save(document, CollectionName);
                await session.SaveChangesAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default) => await SaveAsync(entity, cancellationToken);

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await using var session = SessionProvider.CreateSession();
            var document = Mapper.Map<TDocument>(entity);
            session.Save(document, CollectionName);
            await session.SaveChangesAsync();
        }

        public async Task AddManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var documents = entities.Select(x => Mapper.Map<TDocument>(x));

            await using var session = SessionProvider.CreateSession();
            
            foreach (var document in documents) 
                session.Save(document, CollectionName);
            
            await session.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            await using var session = SessionProvider.CreateSession();
            var document = await FindDocumentAsync(session, entity, cancellationToken);
            session.Delete(document, CollectionName);
            await session.SaveChangesAsync();
        }

        public async Task<int> DeleteManyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            _stopwatch.Restart();
            await using var session = SessionProvider.CreateSession();
            var documents = await Query(session, specification).ListAsync().ToList();
            _stopwatch.Stop();
            Logger.LogDebug("Loading documents to delete took {TimeElapsed}", _stopwatch.Elapsed);

            foreach (var document in documents)
            {
                _stopwatch.Restart();
                session.Delete(document, CollectionName);
                _stopwatch.Stop();
                Logger.LogDebug("Deleting document took {TimeElapsed}", _stopwatch.Elapsed);
            }

            _stopwatch.Restart();
            await session.SaveChangesAsync();
            _stopwatch.Stop();
            Logger.LogDebug("Committing deleted documents took {TimeElapsed}", _stopwatch.Elapsed);
            
            return documents.Count;
        }

        public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            _stopwatch.Restart();
            await using var session = SessionProvider.CreateSession();
            var count = await Query(session, specification).CountAsync();
            _stopwatch.Stop();
            Logger.LogDebug("CountAsync took {TimeElapsed}", _stopwatch.Elapsed);
            return count;
        }

        public async Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            await using var session = SessionProvider.CreateSession();
            var document = await Query(session, specification).FirstOrDefaultAsync()!;
            return Map(document);
        }

        public virtual async Task<IEnumerable<T>> FindManyAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            await using var session = SessionProvider.CreateSession();
            _stopwatch.Restart();
            var documents = await Query(session, specification, orderBy, paging, cancellationToken).ListAsync();
            var mappedDocuments = Map(documents);
            _stopwatch.Stop();
            Logger.LogDebug("FindManyAsync took {TimeElapsed}", _stopwatch.Elapsed);
            return mappedDocuments;
        }

        protected virtual IQuery<TDocument> Query(ISession session, ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            var query = ToQuery(session, specification);

            if (orderBy != null)
                query = OrderBy(query, orderBy, specification);

            if (paging != null)
                query = query.Skip(paging.Skip).Take(paging.Take);

            return query;
        }
        
        protected abstract Task<TDocument?> FindDocumentAsync(ISession session, T entity, CancellationToken cancellationToken);
        protected virtual IQuery<TDocument> OrderBy(IQuery<TDocument> query, IOrderBy<T> orderBy, ISpecification<T> specification) => query;
        protected virtual IQuery<TDocument> ToQuery(ISession session, ISpecification<T> specification) => MapSpecification(session, specification);
        protected abstract IQuery<TDocument> MapSpecification(ISession session, ISpecification<T> specification);

        protected IQuery<TDocument> AutoMapSpecification<TIndex>(ISession session, ISpecification<T> specification) where TIndex : class, IIndex
        {
            var expression = AutoMapSpecification<TIndex>(specification);
            return Query(session, expression);
        }
        
        protected Expression<Func<TIndex, bool>> AutoMapSpecification<TIndex>(ISpecification<T> specification) where TIndex : class, IIndex => specification.ToExpression().ConvertType<T, TDocument>().ConvertType<TDocument, TIndex>();
        protected TDocument Map(T source) => Mapper.Map<TDocument>(source);
        protected T Map(TDocument source) => Mapper.Map<T>(source);
        protected IEnumerable<T> Map(IEnumerable<TDocument> source) => Mapper.Map<IEnumerable<T>>(source);
        protected IQuery<TDocument> Query(ISession session) => session.Query<TDocument>(CollectionName);
        protected IQuery<TDocument, TIndex> Query<TIndex>(ISession session, Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex => session.Query<TDocument, TIndex>(predicate, CollectionName);
        protected IQuery<TDocument, TIndex> Query<TIndex>(ISession session) where TIndex : class, IIndex => session.Query<TDocument, TIndex>(CollectionName);

        public async Task<IEnumerable<TOut>> FindManyAsync<TOut>(ISpecification<T> specification, Expression<Func<T, TOut>> funcMapping, IOrderBy<T>? orderBy = null, IPaging? paging = null, CancellationToken cancellationToken = default) where TOut : class
        {
            await using var session = SessionProvider.CreateSession();
            _stopwatch.Restart();
            var documents = await Query(session, specification, orderBy, paging, cancellationToken).ListAsync();
            var mappedDocuments = Map(documents).AsQueryable().Select(funcMapping);
            _stopwatch.Stop();
            Logger.LogDebug("FindManyAsync took {TimeElapsed}", _stopwatch.Elapsed);
            return mappedDocuments;
        }
    }
}