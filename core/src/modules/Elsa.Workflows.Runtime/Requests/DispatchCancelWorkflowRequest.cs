using Elsa.Workflows;

namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// A dispatch request that indicates that one or more workflows are requested to be cancelled.
/// </summary>
public class DispatchCancelWorkflowRequest
{
    /// <summary>
    /// The ID of the workflow instance to cancel.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;

    /// <summary>
    /// The sub-status to restore if this cancellation fails after <see cref="WorkflowSubStatus.Cancelling"/> was persisted.
    /// Null when no interim mark was written.
    /// </summary>
    public WorkflowSubStatus? PreviousSubStatus { get; set; }

    /// <summary>
    /// The <c>UpdatedAt</c> value written with the Cancelling mark.
    /// Restore matches this stamp so a newer commit is left alone.
    /// </summary>
    public DateTimeOffset? CancellingAt { get; set; }
}