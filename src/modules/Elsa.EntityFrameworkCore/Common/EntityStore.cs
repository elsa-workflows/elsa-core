using Elsa.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Common;

public class EntityStore<TDbContext, TEntity> : Store<TDbContext, TEntity> where TDbContext : DbContext where TEntity : Entity
{
    public EntityStore(IDbContextFactory<TDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }
    
    public async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default) => await SaveAsync(entity, null, cancellationToken);
    public async Task SaveAsync(TEntity entity, Func<TDbContext, TEntity, TEntity>? onSaving, CancellationToken cancellationToken = default) => await SaveAsync(entity, x => x.Id, onSaving, cancellationToken);
    
    public async Task SaveManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => await SaveManyAsync(entities, default, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<TEntity> entities, Func<TDbContext, TEntity, TEntity>? onSaving = default, CancellationToken cancellationToken = default) => await SaveManyAsync(entities, x => x.Id, onSaving, cancellationToken);
}