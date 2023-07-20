using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Filters;

/// <summary>
/// A filter for bookmarks.
/// </summary>
public class BookmarkFilter
{
    /// <summary>
    /// Gets or sets the ID of the bookmark.
    /// </summary>
    public string? BookmarkId { get; set; }
    
    /// <summary>
    /// Gets or sets the IDs of the bookmark.
    /// </summary>
    public ICollection<string>? BookmarkIds { get; set; }
    
    /// <summary>
    /// Gets or sets the IDs of the workflow instance.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }
    
    /// <summary>
    /// Gets or sets the IDs of the workflow instances.
    /// </summary>
    public ICollection<string>? WorkflowInstanceIds { get; set; }
    
    /// <summary>
    /// Gets or sets the hash of the bookmark to find.
    /// </summary>
    public string? Hash { get; set; }
    
    /// <summary>
    /// Gets or sets the correlation ID of the bookmark to find.
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Gets or sets the activity type name of the bookmark to find.
    /// </summary>
    public string? ActivityTypeName { get; set; }
    
    /// <summary>
    /// Gets or sets the activity type names of the bookmarks to find.
    /// </summary>
    public ICollection<string>? ActivityTypeNames { get; set; }

    /// <summary>
    /// Applies the filter to the specified query.
    /// </summary>
    public IQueryable<StoredBookmark> Apply(IQueryable<StoredBookmark> query)
    {
        var filter = this;
        if (filter.BookmarkId != null) query = query.Where(x => x.BookmarkId == filter.BookmarkId);
        if (filter.BookmarkIds != null) query = query.Where(x => filter.BookmarkIds.Contains(x.BookmarkId));
        if (filter.CorrelationId != null) query = query.Where(x => x.CorrelationId == filter.CorrelationId);
        if (filter.Hash != null) query = query.Where(x => x.Hash == filter.Hash);
        if (filter.WorkflowInstanceId != null) query = query.Where(x => x.WorkflowInstanceId == filter.WorkflowInstanceId);
        if (filter.WorkflowInstanceIds != null) query = query.Where(x => filter.WorkflowInstanceIds.Contains(x.WorkflowInstanceId));
        if (filter.ActivityTypeName != null) query = query.Where(x => x.ActivityTypeName == filter.ActivityTypeName);
        if (filter.ActivityTypeNames != null) query = query.Where(x => filter.ActivityTypeNames.Contains(x.ActivityTypeName));

        return query;
    }
}