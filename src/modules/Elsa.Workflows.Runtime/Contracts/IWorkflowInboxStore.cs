using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// A store for workflow inbox messages.
/// </summary>
public interface IWorkflowInboxStore
{
    /// <summary>
    /// Adds a message to the store.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns></returns>
    ValueTask SaveAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default);


    /// <summary>
    /// Finds all messages matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A list of messages that match the filter.</returns>
    ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default);
}