using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Filters;

/// <summary>
/// A filter for bookmark queue items.
/// </summary>
public class BookmarkQueueFilter
{
    /// <summary>
    /// Gets or sets the ID of the bookmark queue item.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Gets or sets the IDs of the bookmark queue items.
    /// </summary>
    public IEnumerable<string>? Ids { get; set; }

    /// <summary>
    /// Gets or sets the ID of the bookmark.
    /// </summary>
    public string? BookmarkId { get; set; }

    /// <summary>
    /// Gets or sets the IDs of the workflow instance.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the bookmark hash of the bookmark queue item to find.
    /// </summary>
    public string? BookmarkHash { get; set; }

    /// <summary>
    /// The ID of the activity instance associated with the bookmark.
    /// </summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// The type name of the activity associated with the bookmark.
    /// </summary>
    public string? ActivityTypeName { get; set; }
    
    /// <summary>
    /// The timestamp less than which the bookmark queue item was created.
    /// </summary>
    public DateTimeOffset? CreatedAtLessThan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the filter is tenant agnostic.
    /// </summary>
    public bool TenantAgnostic { get; set; }

    /// <summary>
    /// Applies the filter to the specified query.
    /// </summary>
    public IQueryable<BookmarkQueueItem> Apply(IQueryable<BookmarkQueueItem> query)
    {
        var filter = this;
        if (filter.Id != null) query = query.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) query = query.Where(x => filter.Ids.Contains(x.Id));
        if (filter.BookmarkId != null) query = query.Where(x => x.BookmarkId == filter.BookmarkId);
        if (filter.BookmarkHash != null) query = query.Where(x => x.StimulusHash == filter.BookmarkHash);
        if (filter.ActivityInstanceId != null) query = query.Where(x => x.ActivityInstanceId == filter.ActivityInstanceId);
        if (filter.ActivityTypeName != null) query = query.Where(x => x.ActivityTypeName == filter.ActivityTypeName);
        if (filter.WorkflowInstanceId != null) query = query.Where(x => x.WorkflowInstanceId == filter.WorkflowInstanceId);
        if (filter.CreatedAtLessThan != null) query = query.Where(x => x.CreatedAt < filter.CreatedAtLessThan);

        return query;
    }
}