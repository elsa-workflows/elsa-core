using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Filters;

/// A filter for bookmark queue items.
public class BookmarkQueueFilter
{
    /// Gets or sets the ID of the bookmark queue item.
    public string? Id { get; set; }

    /// Gets or sets the ID of the bookmark.
    public string? BookmarkId { get; set; }

    /// Gets or sets the IDs of the workflow instance.
    public string? WorkflowInstanceId { get; set; }

    /// Gets or sets the bookmark hash of the bookmark queue item to find.
    public string? BookmarkHash { get; set; }

    /// The ID of the activity instance associated with the bookmark.
    public string? ActivityInstanceId { get; set; }

    /// Applies the filter to the specified query.
    public IQueryable<BookmarkQueueItem> Apply(IQueryable<BookmarkQueueItem> query)
    {
        var filter = this;
        if (filter.Id != null) query = query.Where(x => x.Id == filter.Id);
        if (filter.BookmarkId != null) query = query.Where(x => x.BookmarkId == filter.BookmarkId);
        if (filter.BookmarkHash != null) query = query.Where(x => x.BookmarkHash == filter.BookmarkHash);
        if (filter.ActivityInstanceId != null) query = query.Where(x => x.ActivityInstanceId == filter.ActivityInstanceId);
        if (filter.WorkflowInstanceId != null) query = query.Where(x => x.WorkflowInstanceId == filter.WorkflowInstanceId);

        return query;
    }
}