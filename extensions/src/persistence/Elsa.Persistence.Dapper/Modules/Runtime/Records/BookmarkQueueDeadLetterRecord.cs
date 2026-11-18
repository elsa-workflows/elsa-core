namespace Elsa.Persistence.Dapper.Modules.Runtime.Records;

internal sealed class BookmarkQueueDeadLetterRecord : BookmarkQueueItemRecord
{
    public string OriginalQueueItemId { get; set; } = null!;
    public DateTimeOffset DeadLetteredAt { get; set; }
    public string Reason { get; set; } = null!;
    public bool CanReplay { get; set; }
    public DateTimeOffset? ReplayedAt { get; set; }
    public string? ReplayedQueueItemId { get; set; }
}
