using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Stores;

/// <summary>
/// An in-memory implementation of <see cref="IWorkflowInboxMessageStore"/>.
/// </summary>
public class NoopWorkflowInboxMessageStore : IWorkflowInboxMessageStore
{
    /// <inheritdoc />
    public ValueTask SaveAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        return new(Enumerable.Empty<WorkflowInboxMessage>());
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(IEnumerable<WorkflowInboxMessageFilter> filters, CancellationToken cancellationToken = default)
    {
        return new(Enumerable.Empty<WorkflowInboxMessage>());
    }

    /// <inheritdoc />
    public ValueTask<long> DeleteManyAsync(WorkflowInboxMessageFilter filter, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        return new (0);
    }
}