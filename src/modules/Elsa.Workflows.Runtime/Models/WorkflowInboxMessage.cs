namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// A message that can be delivered to a workflow instance.
/// </summary>
public class WorkflowInboxMessage
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
    public object BookmarkPayload { get; set; } = default!;
    
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
    /// An optional set of inputs to deliver to the workflow instance.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>
    /// Set to true if the message was handled by a workflow instance.
    /// </summary>
    public bool IsHandled { get; set; }

    /// <summary>
    /// The IDs of the workflow instances that handled the message.
    /// </summary>
    public ICollection<string>? AffectedWorkflowInstancesId { get; set; }

    /// <summary>
    /// The date and time the message was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date and time the message was handled.
    /// </summary>
    public DateTimeOffset? HandledAt { get; set; }
}