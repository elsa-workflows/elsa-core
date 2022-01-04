using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Services;

public class EFCoreStore<TEntity> : IStore<TEntity> where TEntity : Entity
{
    private readonly IDbContextFactory<ElsaDbContext> _dbContextFactory;
    private readonly IServiceProvider _serviceProvider;

    public EFCoreStore(IDbContextFactory<ElsaDbContext> dbContextFactory, IServiceProvider serviceProvider)
    {
        _dbContextFactory = dbContextFactory;
        _serviceProvider = serviceProvider;
    }

    public async Task<ElsaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => await _dbContextFactory.CreateDbContextAsync(cancellationToken);

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
        return await set.Where(predicate).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
    }
    
    public async Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        var queryable = query(set.AsQueryable());

        queryable = query(queryable);
        
        return Load(dbContext, queryable).ToList();
    }

    public IEnumerable<TEntity> Load(ElsaDbContext dbContext, IEnumerable<TEntity> entities) => OnLoading(dbContext, entities.ToList());
    

    private void OnSaving(ElsaDbContext dbContext, TEntity entity)
    {
        var handler = _serviceProvider.GetService<IEntitySerializer<TEntity>>();
        handler?.Serialize(dbContext, entity);
    }

    private void OnSaving(ElsaDbContext dbContext, IEnumerable<TEntity> entities)
    {
        var handler = _serviceProvider.GetService<IEntitySerializer<TEntity>>();

        if (handler == null)
            return;

        foreach (var entity in entities)
            handler.Serialize(dbContext, entity);
    }

    private TEntity? OnLoading(ElsaDbContext dbContext, TEntity? entity)
    {
        if (entity == null)
            return null;

        var handler = _serviceProvider.GetService<IEntitySerializer<TEntity>>();
        handler?.Deserialize(dbContext, entity);
        return entity;
    }

    private IEnumerable<TEntity> OnLoading(ElsaDbContext dbContext, ICollection<TEntity> entities)
    {
        var handler = _serviceProvider.GetService<IEntitySerializer<TEntity>>();

        if (handler == null)
            return entities;

        foreach (var entity in entities)
            handler.Deserialize(dbContext, entity);

        return entities;
    }
}