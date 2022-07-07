using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.Common.Models;
using Elsa.Persistence.EntityFrameworkCore.Common.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Common.Implementations;

public class EFCoreStore<TDbContext, TEntity> : IStore<TDbContext, TEntity> where TEntity : Entity where TDbContext : DbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;
    private readonly IServiceProvider _serviceProvider;

    public EFCoreStore(IDbContextFactory<TDbContext> dbContextFactory, IServiceProvider serviceProvider)
    {
        _dbContextFactory = dbContextFactory;
        _serviceProvider = serviceProvider;
    }

    public async Task<TDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => await _dbContextFactory.CreateDbContextAsync(cancellationToken);

    public async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        OnSaving(dbContext, entity);
        await dbContext.BulkInsertOrUpdateAsync(new[] { entity }, config => { config.EnableShadowProperties = true; }, cancellationToken: cancellationToken);
    }

    public async Task SaveManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var entityList = entities.ToList();
        OnSaving(dbContext, entityList);
        await dbContext.BulkInsertOrUpdateAsync(entityList, config => { config.EnableShadowProperties = true; }, cancellationToken: cancellationToken);
    }

    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        var entity = await set.FirstOrDefaultAsync(predicate, cancellationToken);
        return OnLoading(dbContext, entity);
    }

    public async Task<IEnumerable<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        var entities = await set.Where(predicate).ToListAsync(cancellationToken);
        return OnLoading(dbContext, entities);
    }

    public async Task<Page<TEntity>> FindManyAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        OrderDirection orderDirection = OrderDirection.Ascending,
        PageArgs? pageArgs = default,
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
        OnLoading(dbContext, page.Items);
        return page;
    }

    public async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        set.Attach(entity).State = EntityState.Deleted;
        return await dbContext.SaveChangesAsync(cancellationToken) == 1;
    }

    public async Task<int> DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var list = entities.ToList();
        await dbContext.BulkDeleteAsync(list, cancellationToken: cancellationToken);
        return list.Count;
    }

    public async Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        return await set.DeleteWhereAsync(dbContext, predicate, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        var queryable = query(set.AsQueryable());

        queryable = query(queryable);
        var entities = await queryable.ToListAsync(cancellationToken);

        return Load(dbContext, entities).ToList();
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        return await set.AnyAsync(predicate, cancellationToken);
    }

    public IEnumerable<TEntity> Load(TDbContext dbContext, IEnumerable<TEntity> entities) => OnLoading(dbContext, entities.ToList());

    private void OnSaving(TDbContext dbContext, TEntity entity)
    {
        var handler = _serviceProvider.GetService<IEntitySerializer<TDbContext, TEntity>>();
        handler?.Serialize(dbContext, entity);
    }

    private void OnSaving(TDbContext dbContext, IEnumerable<TEntity> entities)
    {
        var handler = _serviceProvider.GetService<IEntitySerializer<TDbContext, TEntity>>();

        if (handler == null)
            return;

        foreach (var entity in entities)
            handler.Serialize(dbContext, entity);
    }

    private TEntity? OnLoading(TDbContext dbContext, TEntity? entity)
    {
        if (entity == null)
            return null;

        var handler = _serviceProvider.GetService<IEntitySerializer<TDbContext, TEntity>>();
        handler?.Deserialize(dbContext, entity);
        return entity;
    }

    private IEnumerable<TEntity> OnLoading(TDbContext dbContext, ICollection<TEntity> entities)
    {
        var handler = _serviceProvider.GetService<IEntitySerializer<TDbContext, TEntity>>();

        if (handler == null)
            return entities;

        foreach (var entity in entities)
            handler.Deserialize(dbContext, entity);

        return entities;
    }
}