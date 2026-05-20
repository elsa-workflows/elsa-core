using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters;

public class ListRequest
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? WorkflowInstanceId { get; set; }
}

public class GetRequest
{
    public string Id { get; set; } = null!;
}

public class DeleteRequest
{
    public string Id { get; set; } = null!;
}

public class ReplayRequest
{
    public string Id { get; set; } = null!;
}

public class ListResponse(ICollection<BookmarkQueueDeadLetterItem> items, long totalCount)
{
    public ICollection<BookmarkQueueDeadLetterItem> Items { get; set; } = items;
    public long TotalCount { get; set; } = totalCount;
}

public class ReplayResponse(bool replayed, string? queueItemId, string? reason)
{
    public bool Replayed { get; set; } = replayed;
    public string? QueueItemId { get; set; } = queueItemId;
    public string? Reason { get; set; } = reason;
}
