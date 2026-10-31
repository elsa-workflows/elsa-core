using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters;

public class ListRequest
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? WorkflowInstanceId { get; set; }
}

public class ListResponse(ICollection<BookmarkQueueDeadLetterModel> items, long totalCount)
{
    public ICollection<BookmarkQueueDeadLetterModel> Items { get; set; } = items;
    public long TotalCount { get; set; } = totalCount;
}

public class BookmarkQueueDeadLetterModel
{
    public string Id { get; set; } = null!;
    public string? TenantId { get; set; }
    public string OriginalQueueItemId { get; set; } = null!;
    public string? WorkflowInstanceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? BookmarkId { get; set; }
    public string? StimulusHash { get; set; }
    public string? ActivityInstanceId { get; set; }
    public string? ActivityTypeName { get; set; }
    public DateTimeOffset OriginalCreatedAt { get; set; }
    public DateTimeOffset DeadLetteredAt { get; set; }
    public string Reason { get; set; } = null!;
    public int DeliveryAttempts { get; set; }
    public DateTimeOffset? LastAttemptedAt { get; set; }
    public string? LastErrorType { get; set; }
    public string? LastErrorMessage { get; set; }
    public bool CanReplay { get; set; }
    public DateTimeOffset? ReplayedAt { get; set; }
    public string? ReplayedQueueItemId { get; set; }

    public static BookmarkQueueDeadLetterModel FromEntity(BookmarkQueueDeadLetterItem item) => new()
    {
        Id = item.Id,
        TenantId = item.TenantId,
        OriginalQueueItemId = item.OriginalQueueItemId,
        WorkflowInstanceId = item.WorkflowInstanceId,
        CorrelationId = item.CorrelationId,
        BookmarkId = item.BookmarkId,
        StimulusHash = item.StimulusHash,
        ActivityInstanceId = item.ActivityInstanceId,
        ActivityTypeName = item.ActivityTypeName,
        OriginalCreatedAt = item.OriginalCreatedAt,
        DeadLetteredAt = item.DeadLetteredAt,
        Reason = item.Reason,
        DeliveryAttempts = item.DeliveryAttempts,
        LastAttemptedAt = item.LastAttemptedAt,
        LastErrorType = item.LastErrorType,
        LastErrorMessage = item.LastErrorMessage,
        CanReplay = item.CanReplay,
        ReplayedAt = item.ReplayedAt,
        ReplayedQueueItemId = item.ReplayedQueueItemId
    };
}

public class ReplayResponse(bool replayed, string? queueItemId, string? reason)
{
    public bool Replayed { get; set; } = replayed;
    public string? QueueItemId { get; set; } = queueItemId;
    public string? Reason { get; set; } = reason;
}
