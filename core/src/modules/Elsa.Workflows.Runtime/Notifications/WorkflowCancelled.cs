using Elsa.Mediator.Contracts;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Published after a workflow instance is cancelled through <see cref="IWorkflowCanceler"/>.
/// </summary>
/// <param name="WorkflowInstanceId">The ID of the cancelled workflow instance.</param>
public record WorkflowCancelled(string WorkflowInstanceId) : INotification
{
    /// <summary>
    /// The workflow instance's state after the cancellation: its status, sub-status, output and properties. <c>null</c> when
    /// the publisher did not supply it.
    /// </summary>
    public WorkflowState? WorkflowState { get; init; }
}
