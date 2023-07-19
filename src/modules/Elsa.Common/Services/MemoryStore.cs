using System.Collections.Concurrent;
using Elsa.Common.Entities;

namespace Elsa.Common.Services;

/// <summary>
/// A simple in-memory store for entities.
/// </summary>
/// <typeparam name="TEntity">The type of entity to store.</typeparam>
public class MemoryStore<TEntity>
{
    private IDictionary<string, TEntity> Entities { get; set; } = new ConcurrentDictionary<string, TEntity>();
    
    /// <summary>
    /// Adds or updates an entity.
    /// </summary>
    /// <param name="entity">The entity to add or update.</param>
    /// <param name="idAccessor">A function that returns the ID of the entity.</param>
    public void Save(TEntity entity, Func<TEntity, string> idAccessor) => Entities[idAccessor(entity)] = entity;

    /// <summary>
    /// Adds or updates many entities.
    /// </summary>
    /// <param name="entities">The entities to add or update.</param>
    /// <param name="idAccessor">A function that returns the ID of the entity.</param>
    public void SaveMany(IEnumerable<TEntity> entities, Func<TEntity, string> idAccessor)
    {
        foreach (var entity in entities)
            Save(entity, idAccessor);
    }

    /// <summary>
    /// Finds an entity matching the specified predicate.
    /// </summary>
    /// <param name="predicate">A predicate to match.</param>
    /// <returns>The matching entity or null if no match was found.</returns>
    public TEntity? Find(Func<TEntity, bool> predicate) => Entities.Values.Where(predicate).FirstOrDefault();
    
    /// <summary>
    /// Finds all entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">A predicate to match.</param>
    /// <returns>The matching entities.</returns>
    public IEnumerable<TEntity> FindMany(Func<TEntity, bool> predicate) => Entities.Values.Where(predicate);
    
    /// <summary>
    /// Finds all entities matching the specified predicate and orders them by the specified key.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="orderBy">The key to order by.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <returns>The matching entities.</returns>
    public IEnumerable<TEntity> FindMany<TKey>(Func<TEntity, bool> predicate, Func<TEntity, TKey> orderBy, OrderDirection orderDirection = OrderDirection.Ascending)
    {
        var query = Entities.Values.Where(predicate);

        query = orderDirection switch
        {
            OrderDirection.Ascending => query.OrderBy(orderBy),
            OrderDirection.Descending => query.OrderByDescending(orderBy),
            _ => query.OrderBy(orderBy)
        };
        
        return query;
    }

    /// <summary>
    /// Lists all entities.
    /// </summary>
    /// <returns>All entities.</returns>
    public IEnumerable<TEntity> List() => Entities.Values;

    /// <summary>
    /// Deletes an entity by ID.
    /// </summary>
    /// <param name="id">The ID of the entity to delete.</param>
    /// <returns>True if the entity was deleted, otherwise false.</returns>
    public bool Delete(string id) => Entities.Remove(id);

    /// <summary>
    /// Deletes all entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The number of entities deleted.</returns>
    public long DeleteWhere(Func<TEntity, bool> predicate)
    {
        var query =
            from entry in Entities
            where predicate(entry.Value)
            select entry;

        var entries = query.ToList();
        foreach (var entry in entries)
            Entities.Remove(entry);

        return entries.LongCount();
    }

    /// <summary>
    /// Deletes all entities matching the specified IDs.
    /// </summary>
    /// <param name="ids">The IDs of the entities to delete.</param>
    /// <returns>The number of entities deleted.</returns>
    public long DeleteMany(IEnumerable<string> ids)
    {
        var count = 0;
        foreach (var id in ids)
        {
            count++;
            Entities.Remove(id);
        }

        return count;
    }

    /// <summary>
    /// Deletes the specified entities. 
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="idAccessor">A function that returns the ID of the entity.</param>
    /// <returns></returns>
    public long DeleteMany(IEnumerable<TEntity> entities, Func<TEntity, string> idAccessor)
    {
        var count = 0;
        var list = entities.ToList();

        foreach (var entity in list)
        {
            count++;
            var id = idAccessor(entity);
            Entities.Remove(id);
        }

        return count;
    }

    /// <summary>
    /// Returns a queryable of all entities.
    /// </summary>
    /// <param name="query">A function that returns a queryable.</param>
    /// <returns>A queryable of all entities.</returns>
    public IEnumerable<TEntity> Query(Func<IQueryable<TEntity>, IQueryable<TEntity>> query)
    {
        var queryable = Entities.Values.AsQueryable();
        return query(queryable);
    }

    /// <summary>
    /// Returns true if any entity matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>True if any entity matches the specified predicate, otherwise false.</returns>
    public bool Any(Func<TEntity, bool> predicate) => Entities.Values.Any(predicate);

    /// <summary>
    /// Returns the number of entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="propertySelector">The property to distinct by.</param>
    /// <returns>True if any entity matches the specified predicate, otherwise false.</returns>
    public long Count<TProperty>(Func<TEntity, bool> predicate, Func<TEntity, TProperty> propertySelector)
    {
        return Entities.Values
            .DistinctBy(propertySelector)
            .Count(predicate);
    }
}