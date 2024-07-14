using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Runtime.Records;

internal class BookmarkQueueItemRecord : Record
{
    public string WorkflowInstanceId { get; set; } = default!;
    public string? BookmarkId { get; set; } = default!;
    public string? BookmarkHash { get; set; } = default!;
    public string? SerializedOptions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}