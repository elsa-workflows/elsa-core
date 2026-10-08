using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Filters;

/// <summary>
/// A filter that can be used to find messages in the workflow inbox.
/// </summary>
public class WorkflowInboxMessageFilter
{
    /// <summary>
    /// The activity type name to filter by.
    /// </summary>
    public string? ActivityTypeName { get; set; }

    /// <summary>
    /// The hash of the bookmark payload to filter by.
    /// </summary>
    public string? Hash { get; set; }

    /// <summary>
    /// The ID of the workflow instance to filter by.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// The correlation ID of the workflow instance to filter by.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The ID of the activity instance to filter by.
    /// </summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// A flag indicating whether to filter by messages that have expired.
    /// </summary>
    public bool? IsExpired { get; set; }

    /// <summary>
    /// Applies the filter to the specified query.
    /// </summary>
    /// <returns>The filtered query.</returns>
    public IQueryable<WorkflowInboxMessage> Apply(IQueryable<WorkflowInboxMessage> query, DateTimeOffset now)
    {
        var filter = this;
        if (filter.CorrelationId != null) query = query.Where(x => filter.CorrelationId == x.CorrelationId);
        if (filter.WorkflowInstanceId != null) query = query.Where(x => filter.WorkflowInstanceId == x.WorkflowInstanceId);
        if (filter.Hash != null) query = query.Where(x => filter.Hash == x.Hash);
        if (filter.ActivityTypeName != null) query = query.Where(x => filter.ActivityTypeName == x.ActivityTypeName);
        if (filter.ActivityInstanceId != null) query = query.Where(x => filter.ActivityInstanceId == x.ActivityInstanceId);
        if (filter.IsExpired != null) query = query.Where(x => x.ExpiresAt <= now);

        return query;
    }
}