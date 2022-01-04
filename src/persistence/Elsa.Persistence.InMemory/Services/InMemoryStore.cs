using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.InMemory.Services;

public class InMemoryStore<TEntity> where TEntity : Entity
{
    private IDictionary<string, TEntity> Entities { get; set; } = new Dictionary<string, TEntity>();
    public void Save(TEntity entity) => Entities[entity.Id] = entity;

    public void SaveMany(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities) 
            Save(entity);
    }

    public TEntity? Find(Func<TEntity, bool> predicate) => Entities.Values.Where(predicate).FirstOrDefault();
    public IEnumerable<TEntity> FindMany(Func<TEntity, bool> predicate) => Entities.Values.Where(predicate);
    public IEnumerable<TEntity> List() => Entities.Values;

    public void Delete(string id) => Entities.Remove(id);

    public int DeleteWhere(Func<TEntity, bool> predicate)
    {
        var query =
            from entry in Entities
            where predicate(entry.Value)
            select entry;

        var entries = query.ToList();
        foreach (var entry in entries)
            Entities.Remove(entry);

        return entries.Count;
    }

    public int DeleteMany(IEnumerable<string> ids)
    {
        var count = 0;
        foreach (var id in ids)
        {
            count++;
            Entities.Remove(id);
        }

        return count;
    }
}