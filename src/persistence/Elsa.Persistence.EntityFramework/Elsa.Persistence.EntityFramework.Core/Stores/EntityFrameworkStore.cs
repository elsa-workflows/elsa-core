using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public abstract class EntityFrameworkStore<T, TEntity> : IStore<T> where T : IEntity where TEntity : class, IEntity
    {
        private readonly SemaphoreSlim _semaphore = new(1);

        protected EntityFrameworkStore(ElsaContext dbContext, IMapper mapper)
        {
            DbContext = dbContext;
            Mapper = mapper;
        }

        protected ElsaContext DbContext { get; }
        protected IMapper Mapper { get; }
        protected abstract DbSet<TEntity> DbSet { get; }

        public async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                var existingEntity = await DbSet.FindAsync(new object[] { entity.Id }, cancellationToken);
                var mappedEntity = Mapper.Map(entity, existingEntity);

                if (existingEntity == null!)
                    await DbSet.AddAsync(mappedEntity, cancellationToken);

                await DbContext.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
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
                var orderByExpression = orderBy.OrderByExpression.ConvertType<T, TEntity>();
                queryable = orderBy.SortDirection == SortDirection.Ascending ? queryable.OrderBy(orderByExpression) : queryable.OrderByDescending(orderByExpression);
            }

            if (paging != null)
                queryable = queryable.Skip(paging.Skip).Take(paging.Take);

            var entities = await queryable.ToListAsync(cancellationToken);
            return Mapper.Map<IEnumerable<T>>(entities);
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
            return Mapper.Map<T>(entity);
        }

        protected abstract Expression<Func<TEntity, bool>> MapSpecification(ISpecification<T> specification);

        protected Expression<Func<TEntity, bool>> AutoMapSpecification(ISpecification<T> specification) => specification.ToExpression().ConvertType<T, TEntity>();
    }
}