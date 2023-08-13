using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// A store for workflow inbox messages.
/// </summary>
public interface IWorkflowInboxMessageStore
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
    
    /// <summary>
    /// Finds all messages matching the specified filters.
    /// </summary>
    /// <param name="filters">The filters to apply.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A list of messages that match the filter.</returns>
    ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(IEnumerable<WorkflowInboxMessageFilter> filters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all messages matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="pageArgs">An optional page arguments.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The number of deleted messages.</returns>
    ValueTask<long> DeleteManyAsync(WorkflowInboxMessageFilter filter, PageArgs? pageArgs = default, CancellationToken cancellationToken = default);
}