using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Runtime.Records;

internal class StoredBookmarkRecord : Record
{
    public string ActivityTypeName { get; set; } = default!;
    public string Hash { get; set; } = default!;
    public string WorkflowInstanceId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public string? SerializedPayload { get; set; }
    public string? SerializedMetadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}