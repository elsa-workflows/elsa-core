using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Contracts;

public class TriggerFilter
{
    public string? Id { get; set; }
    public ICollection<string>? Ids { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    public ICollection<string>? WorkflowDefinitionIds { get; set; }
    public string? Name { get; set; }
    public ICollection<string>? Names { get; set; }
    public string? Hash { get; set; }

    public IQueryable<StoredTrigger> Apply(IQueryable<StoredTrigger> queryable)
    {
        if (Id != null) queryable = queryable.Where(x => x.Id == Id);
        if (Ids != null) queryable = queryable.Where(x => Ids.Contains(x.Id));
        if (WorkflowDefinitionId != null) queryable = queryable.Where(x => x.WorkflowDefinitionId == WorkflowDefinitionId);
        if (WorkflowDefinitionIds != null) queryable = queryable.Where(x => WorkflowDefinitionIds.Contains(x.WorkflowDefinitionId));
        if (Name != null) queryable = queryable.Where(x => x.Name == Name);
        if (Names != null) queryable = queryable.Where(x => Names.Contains(x.Name));
        if (Hash != null) queryable = queryable.Where(x => x.Hash == Hash);
        return queryable;
    }
}

/// <summary>
/// Provides access to the underlying store of stored triggers.
/// </summary>
public interface ITriggerStore
{
    /// <summary>
    /// Adds or updates the specified record.
    /// </summary>
    ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates the specified set of records. 
    /// </summary>
    ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all records matching the specified filter.
    /// </summary>
    ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a set of records based on the specified removed and added records.
    /// </summary>
    ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all records matching the specified filter.
    /// </summary>
    ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default);
}