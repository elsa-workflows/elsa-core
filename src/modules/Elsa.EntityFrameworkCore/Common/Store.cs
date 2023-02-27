using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Common;

public class Store<TDbContext, TEntity> where TDbContext : DbContext where TEntity : class
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;

    public Store(IDbContextFactory<TDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<TDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    
    public async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default) => await SaveAsync(entity, default, default, cancellationToken);
    
    public async Task SaveAsync(TEntity entity, Expression<Func<TEntity, object>>? uniqueField = default, CancellationToken cancellationToken = default) => await SaveAsync(entity, uniqueField, default, cancellationToken);
    
    public async Task SaveAsync(TEntity entity, Func<TDbContext, TEntity, TEntity>? onSaving = default, CancellationToken cancellationToken = default) => await SaveAsync(entity, default, onSaving, cancellationToken);

    public async Task SaveAsync(TEntity entity, Expression<Func<TEntity, object>>? uniqueField = default, Func<TDbContext, TEntity, TEntity>? onSaving = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        entity = onSaving?.Invoke(dbContext, entity) ?? entity;
        await dbContext.BulkUpsertAsync(new[] {entity}, uniqueField, cancellationToken);
    }

    public async Task SaveManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => await SaveManyAsync(entities, default, default, cancellationToken);
    
    public async Task SaveManyAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>>? uniqueField = default, CancellationToken cancellationToken = default) => await SaveManyAsync(entities, uniqueField, default, cancellationToken);
    
    public async Task SaveManyAsync(IEnumerable<TEntity> entities, Func<TDbContext, TEntity, TEntity>? onSaving = default, CancellationToken cancellationToken = default) => await SaveManyAsync(entities, default, onSaving, cancellationToken);
    
    public async Task SaveManyAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>>? uniqueField = default, Func<TDbContext, TEntity, TEntity>? onSaving = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var entityList = entities.ToList();

        if (onSaving != null)
            entityList = entityList.Select(x => onSaving(dbContext, x)).ToList();

        await dbContext.BulkUpsertAsync(entityList, uniqueField, cancellationToken);
    }

    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) => await FindAsync(predicate, default, cancellationToken);

    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<TDbContext, TEntity?, TEntity?>? onLoading = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        var entity = await set.FirstOrDefaultAsync(predicate, cancellationToken);

        if (entity == null)
            return null;

        if(onLoading != default)
            entity = onLoading.Invoke(dbContext, entity);

        return entity;
    }

    public async Task<IEnumerable<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) => await FindManyAsync(predicate, default, cancellationToken);

    public async Task<IEnumerable<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate, Func<TDbContext, TEntity?, TEntity?>? onLoading = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        var entities = await set.Where(predicate).ToListAsync(cancellationToken);

        if (onLoading != null)
            entities = entities.Select(x => onLoading(dbContext, x)!).ToList();

        return entities;
    }

    public async Task<Page<TEntity>> FindManyAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        OrderDirection orderDirection = OrderDirection.Ascending,
        PageArgs? pageArgs = default,
        CancellationToken cancellationToken = default) =>
        await FindManyAsync(predicate, orderBy, orderDirection, pageArgs, default, cancellationToken);

    public async Task<Page<TEntity>> FindManyAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        OrderDirection orderDirection = OrderDirection.Ascending,
        PageArgs? pageArgs = default,
        Func<TDbContext, TEntity?, TEntity?>? onLoading = default,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().Where(predicate);

        set = orderDirection switch
        {
            OrderDirection.Ascending => set.OrderBy(orderBy),
            OrderDirection.Descending => set.OrderByDescending(orderBy),
            _ => set.OrderBy(orderBy)
        };

        var page = await set.PaginateAsync(pageArgs);

        if (onLoading != null)
            page = new Page<TEntity>(page.Items.Select(x => onLoading(dbContext, x)!).ToList(), page.TotalCount);

        return page;
    }

    public async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        set.Attach(entity).State = EntityState.Deleted;
        return await dbContext.SaveChangesAsync(cancellationToken) == 1;
    }

    public async Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        return await set.Where(predicate).ExecuteDeleteAsync(cancellationToken);
    }
    
    public async Task<int> DeleteWhereAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        var queryable = query(set.AsQueryable());
        var expression = Expression.Lambda<Func<TEntity, bool>>(queryable.Expression);
        return await set.Where(expression).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default) => await QueryAsync(query, default, cancellationToken);

    public async Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Func<TDbContext, TEntity?, TEntity?>? onLoading = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        var queryable = query(set.AsQueryable());

        queryable = query(queryable);
        var entities = await queryable.ToListAsync(cancellationToken);

        if (onLoading != null)
            entities = entities.Select(x => onLoading(dbContext, x)!).ToList();

        return entities;
    }
    
    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Expression<Func<TEntity, TResult>> selector, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        var queryable = query(set.AsQueryable());

        queryable = query(queryable);
        return await queryable.Select(selector).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        return await set.AnyAsync(predicate, cancellationToken);
    }
}