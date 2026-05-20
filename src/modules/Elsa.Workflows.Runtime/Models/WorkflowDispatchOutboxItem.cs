using Elsa.Workflows.Runtime.Commands;

namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// A durable workflow dispatch command waiting for delivery.
/// </summary>
public class WorkflowDispatchOutboxItem
{
    /// <summary>
    /// The outbox item ID.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// The workflow instance whose committed state must reference this item before it can be delivered.
    /// </summary>
    public string OwnerWorkflowInstanceId { get; set; } = null!;

    /// <summary>
    /// The tenant ID captured when the item was enqueued.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// The kind of dispatch command stored by this item.
    /// </summary>
    public WorkflowDispatchOutboxItemKind Kind { get; set; }

    /// <summary>
    /// When the item was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// A workflow definition dispatch command.
    /// </summary>
    public DispatchWorkflowDefinitionCommand? WorkflowDefinitionCommand { get; set; }

    /// <summary>
    /// A workflow instance dispatch command.
    /// </summary>
    public DispatchWorkflowInstanceCommand? WorkflowInstanceCommand { get; set; }

    /// <summary>
    /// A trigger workflow dispatch command.
    /// </summary>
    public DispatchTriggerWorkflowsCommand? TriggerWorkflowsCommand { get; set; }

    /// <summary>
    /// A resume workflows dispatch command.
    /// </summary>
    public DispatchResumeWorkflowsCommand? ResumeWorkflowsCommand { get; set; }
}
