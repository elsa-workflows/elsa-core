using Elsa.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Common;

/// <summary>
/// A generic repository class around EF Core for accessing entities that inherit from <see cref="Entity"/>.
/// </summary>
/// <typeparam name="TDbContext">The type of the database context.</typeparam>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public class EntityStore<TDbContext, TEntity> : Store<TDbContext, TEntity> where TDbContext : DbContext where TEntity : Entity
{
    /// <inheritdoc />
    public EntityStore(IDbContextFactory<TDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }

    /// <summary>
    /// Saves the entity.
    /// </summary>
    /// <param name="entity">The entity to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default) => await SaveAsync(entity, null, cancellationToken);
    
    /// <summary>
    /// Saves the entity.
    /// </summary>
    /// <param name="entity">The entity to save.</param>
    /// <param name="onSaving">The callback to invoke before saving the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveAsync(TEntity entity, Func<TDbContext, TEntity, CancellationToken, ValueTask<TEntity>>? onSaving, CancellationToken cancellationToken = default) => await SaveAsync(entity, x => x.Id, onSaving, cancellationToken);
    
    /// <summary>
    /// Saves the specified entities.
    /// </summary>
    /// <param name="entities">The entities to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => await SaveManyAsync(entities, default, cancellationToken);
    
    /// <summary>
    /// Saves the specified entities.
    /// </summary>
    /// <param name="entities">The entities to save.</param>
    /// <param name="onSaving">The callback to invoke before saving the entities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveManyAsync(IEnumerable<TEntity> entities, Func<TDbContext, TEntity, CancellationToken, ValueTask<TEntity>>? onSaving = default, CancellationToken cancellationToken = default) => await SaveManyAsync(entities, x => x.Id, onSaving, cancellationToken);
}