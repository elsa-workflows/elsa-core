using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Runtime.Records;

internal class BookmarkQueueItemRecord : Record
{
    public string? WorkflowInstanceId { get; set; }
    public string? BookmarkId { get; set; }
    public string? StimulusHash { get; set; }
    public string? ActivityInstanceId { get; set; }
    public string? ActivityTypeName { get; set; }
    public string? SerializedOptions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}