using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Filters;

/// <summary>
/// A filter for bookmark queue dead-letter items.
/// </summary>
public class BookmarkQueueDeadLetterFilter
{
    /// <summary>
    /// Gets or sets the ID of the dead-letter item.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the IDs of the dead-letter items.
    /// </summary>
    public IEnumerable<string>? Ids { get; set; }

    /// <summary>
    /// Gets or sets the original queue item ID.
    /// </summary>
    public string? OriginalQueueItemId { get; set; }

    /// <summary>
    /// Gets or sets the original queue item IDs.
    /// </summary>
    public IEnumerable<string>? OriginalQueueItemIds { get; set; }

    /// <summary>
    /// Gets or sets the ID of the workflow instance.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// The timestamp less than which the dead-letter item was created.
    /// </summary>
    public DateTimeOffset? DeadLetteredAtLessThan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the filter is tenant agnostic.
    /// </summary>
    public bool TenantAgnostic { get; set; }

    /// <summary>
    /// Applies the filter to the specified query.
    /// </summary>
    public IQueryable<BookmarkQueueDeadLetterItem> Apply(IQueryable<BookmarkQueueDeadLetterItem> query)
    {
        var filter = this;
        if (filter.Id != null) query = query.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) query = query.Where(x => filter.Ids.Contains(x.Id));
        if (filter.OriginalQueueItemId != null) query = query.Where(x => x.OriginalQueueItemId == filter.OriginalQueueItemId);
        if (filter.OriginalQueueItemIds != null) query = query.Where(x => filter.OriginalQueueItemIds.Contains(x.OriginalQueueItemId));
        if (filter.WorkflowInstanceId != null) query = query.Where(x => x.WorkflowInstanceId == filter.WorkflowInstanceId);
        if (filter.DeadLetteredAtLessThan != null) query = query.Where(x => x.DeadLetteredAt < filter.DeadLetteredAtLessThan);

        return query;
    }
}
