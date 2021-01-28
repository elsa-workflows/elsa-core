using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public abstract class EntityFrameworkStore<T> : IStore<T> where T : class, IEntity
    {
        private readonly SemaphoreSlim _semaphore = new(1);

        protected EntityFrameworkStore(ElsaContext dbContext)
        {
            DbContext = dbContext;
        }

        protected ElsaContext DbContext { get; }
        protected abstract DbSet<T> DbSet { get; }

        public async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                var existingEntity = await DbSet.FindAsync(new object[] { entity.Id }, cancellationToken);

                if (existingEntity == null!)
                    await DbSet.AddAsync(entity, cancellationToken);

                OnSaving(entity);
                await DbContext.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(entity, cancellationToken);
            OnSaving(entity);
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        
        public async Task AddManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();
            await DbSet.AddRangeAsync(list, cancellationToken);
            
            foreach (var entity in list) 
                OnSaving(entity);
            
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            OnSaving(entity);
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            await DbSet.DeleteByKeyAsync(cancellationToken, entity.Id);
        }

        public async Task<int> DeleteManyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var filter = MapSpecification(specification);
            return await DbSet.Where(filter).DeleteFromQueryAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> FindManyAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            var filter = MapSpecification(specification);
            var queryable = DbSet.Where(filter);

            if (orderBy != null)
            {
                var orderByExpression = orderBy.OrderByExpression;
                queryable = orderBy.SortDirection == SortDirection.Ascending ? queryable.OrderBy(orderByExpression) : queryable.OrderByDescending(orderByExpression);
            }

            if (paging != null)
                queryable = queryable.Skip(paging.Skip).Take(paging.Take);

            return (await queryable.ToListAsync(cancellationToken)).Select(ReadShadowProperties).ToList();
        }

        private T ReadShadowProperties(T entity)
        {
            OnLoading(entity);
            return entity;
        }

        public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var filter = MapSpecification(specification);
            return DbSet.CountAsync(filter, cancellationToken);
        }

        public async Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var filter = MapSpecification(specification);
            var entity = await DbSet.FirstOrDefaultAsync(filter, cancellationToken);
            return entity != null ? ReadShadowProperties(entity) : default;
        }

        protected virtual void OnSaving(T entity)
        {
        }

        protected virtual void OnLoading(T entity)
        {
        }

        protected abstract Expression<Func<T, bool>> MapSpecification(ISpecification<T> specification);

        protected Expression<Func<T, bool>> AutoMapSpecification(ISpecification<T> specification) => specification.ToExpression();
    }
}