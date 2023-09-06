namespace Elsa.Dapper.Modules.Runtime.Records;

/// <summary>
/// A message that can be delivered to a workflow instance.
/// </summary>
public class WorkflowInboxMessageRecord
{
    /// <summary>
    /// The ID of the message.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// The type name of the activity to deliver the message to.
    /// </summary>
    public string ActivityTypeName { get; set; } = default!;

    /// <summary>
    /// An optional bookmark payload that can be used to filter the workflow instances to deliver the message to.
    /// </summary>
    public string SerializedBookmarkPayload { get; set; } = default!;
    
    /// <summary>
    /// The hash of the bookmark.
    /// </summary>
    public string Hash { get; set; } = default!;

    /// <summary>
    /// An optional workflow instance ID to select the workflow instance to deliver the message to.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// An optional correlation ID to select the workflow instance to deliver the message to.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// An optional activity instance ID to select the workflow instance to deliver the message to.
    /// </summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// An optional set of inputs to deliver to the workflow instance.
    /// </summary>
    public string? SerializedInput { get; set; }

    /// <summary>
    /// The date and time the message was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date and time the message expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}