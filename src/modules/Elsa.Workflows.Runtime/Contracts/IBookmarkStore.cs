using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Contracts;

public class BookmarkFilter
{
    public string? WorkflowInstanceId { get; set; }
    public string? Hash { get; set; }
    public string? CorrelationId { get; set; }
    public string? ActivityTypeName { get; set; }
    public ICollection<string>? ActivityTypeNames { get; set; }

    public IQueryable<StoredBookmark> Apply(IQueryable<StoredBookmark> query)
    {
        var filter = this;
        if (filter.CorrelationId != null) query = query.Where(x => x.CorrelationId == filter.CorrelationId);
        if (filter.Hash != null) query = query.Where(x => x.Hash == filter.Hash);
        if (filter.WorkflowInstanceId != null) query = query.Where(x => x.WorkflowInstanceId == filter.WorkflowInstanceId);
        if (filter.ActivityTypeName != null) query = query.Where(x => x.ActivityTypeName == filter.ActivityTypeName);
        if (filter.ActivityTypeNames != null) query = query.Where(x => filter.ActivityTypeNames.Contains(x.ActivityTypeName));

        return query;
    }
}

/// <summary>
/// Provides access to stored bookmarks.
/// </summary>
public interface IBookmarkStore
{
    /// <summary>
    /// Adds or updates the specified bookmark. 
    /// </summary>
    ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a set of bookmarks matching the specified filter.
    /// </summary>
    ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a set of bookmarks matching the specified filter.
    /// </summary>
    ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default);
}