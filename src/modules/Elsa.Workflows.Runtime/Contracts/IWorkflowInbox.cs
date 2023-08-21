using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// An inbox for delivering messages to workflow instances.
/// </summary>
public interface IWorkflowInbox
{
    /// <summary>
    /// Stores a message for a workflow instance.
    /// </summary>
    /// <param name="message">The message to deliver.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    ValueTask<SubmitWorkflowInboxMessageResult> SubmitAsync(NewWorkflowInboxMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stores a message for a workflow instance.
    /// </summary>
    /// <param name="message">The message to store.</param>
    /// <param name="options">An optional set of delivery options.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    ValueTask<SubmitWorkflowInboxMessageResult> SubmitAsync(NewWorkflowInboxMessage message, WorkflowInboxMessageDeliveryOptions options, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delivers a message to a workflow instance.
    /// </summary>
    /// <param name="message">The message to deliver.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    ValueTask<DeliverWorkflowInboxMessageResult> DeliverAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Broadcasts the specified message, which may trigger new workflows and resume existing ones.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    ValueTask<DeliverWorkflowInboxMessageResult> BroadcastAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all messages matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A list of messages that match the filter.</returns>
    ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds all messages matching the specified filter.
    /// </summary>
    /// <param name="filters">The filters to apply.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A list of messages that match the filter.</returns>
    ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(IEnumerable<WorkflowInboxMessageFilter> filters, CancellationToken cancellationToken = default);
}