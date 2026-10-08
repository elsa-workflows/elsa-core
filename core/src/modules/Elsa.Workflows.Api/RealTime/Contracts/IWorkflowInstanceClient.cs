using Elsa.Workflows.Api.RealTime.Messages;

namespace Elsa.Workflows.Api.RealTime.Contracts;

/// <summary>
/// Represents a client for receiving workflow events on the client.
/// </summary>
public interface IWorkflowInstanceClient
{
    /// <summary>
    /// Notifies the client that an activity is executing.
    /// </summary>
    /// <param name="message">The message containing activity information to send.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task ActivityExecutingAsync(ActivityExecutingMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Notifies the client that an activity has executed.
    /// </summary>
    /// <param name="message">The message containing activity information to send.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task ActivityExecutedAsync(ActivityExecutedMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies the client that activity execution log entries for a given workflow instance have been updated.
    /// </summary>
    /// <param name="message">The message containing the workflow instance ID.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task ActivityExecutionLogUpdatedAsync(ActivityExecutionLogUpdatedMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Notifies the client that the workflow execution log has been updated. 
    /// </summary>
    /// <param name="message">Tge message representing the workflow execution log update.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task WorkflowExecutionLogUpdatedAsync(WorkflowExecutionLogUpdatedMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Notifies the client that the workflow instance was updated.
    /// </summary>
    /// <param name="message">The message containing the updated workflow instance ID.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns></returns>
    Task WorkflowInstanceUpdatedAsync(WorkflowInstanceUpdatedMessage message, CancellationToken cancellationToken = default);
}