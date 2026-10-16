using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWorkflowsCancellation;

/// <summary>
/// Shared count of cancel-cascade page reads the failing store rejected. Kept outside the store because the host
/// builds a new store instance per resolution.
/// </summary>
public sealed class RejectedCascadeReadCounter
{
    public int Count { get; set; }
}

/// <summary>
/// Delegates to the real store, except the first <paramref name="failureCount"/> paged, ordered reads of a parent's
/// children — the query the cancel cascade uses — throw. Later cascade reads after that budget is spent, and every
/// other store call, still go through.
/// </summary>
public class FailingOncePagedReadWorkflowInstanceStore(
    IWorkflowInstanceStore inner,
    int failureCount = 1,
    RejectedCascadeReadCounter? rejectedReads = null,
    bool throwCanceled = false) : IWorkflowInstanceStore
{
    private int _failures;

    public ValueTask<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        inner.FindAsync(filter, cancellationToken);

    public ValueTask<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default) =>
        inner.FindManyAsync(filter, pageArgs, cancellationToken);

    public ValueTask<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        // The cancel cascade queries by parent id only. Arrange/assert polls set DefinitionId, so they are left alone.
        if (_failures < failureCount && filter.DefinitionId == null && filter.ParentWorkflowInstanceIds is { Count: > 0 })
        {
            _failures++;
            if (rejectedReads is not null)
                rejectedReads.Count++;

            throw throwCanceled
                ? new OperationCanceledException("Simulated unrelated child page read cancellation.")
                : new InvalidOperationException("Simulated child page read failure.");
        }

        return inner.FindManyAsync(filter, pageArgs, order, cancellationToken);
    }

    public ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        inner.FindManyAsync(filter, cancellationToken);

    public ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        inner.FindManyAsync(filter, order, cancellationToken);

    public ValueTask<long> CountAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        inner.CountAsync(filter, cancellationToken);

    public ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default) =>
        inner.SummarizeManyAsync(filter, pageArgs, cancellationToken);

    public ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        inner.SummarizeManyAsync(filter, pageArgs, order, cancellationToken);

    public ValueTask<IEnumerable<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        inner.FindManyIdsAsync(filter, cancellationToken);

    public ValueTask<Page<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default) =>
        inner.FindManyIdsAsync(filter, pageArgs, cancellationToken);

    public ValueTask<Page<string>> FindManyIdsAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        inner.FindManyIdsAsync(filter, pageArgs, order, cancellationToken);

    public ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        inner.SummarizeManyAsync(filter, cancellationToken);

    public ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrder>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrder> order, CancellationToken cancellationToken = default) =>
        inner.SummarizeManyAsync(filter, order, cancellationToken);

    public ValueTask SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default) =>
        inner.SaveAsync(instance, cancellationToken);

    public ValueTask AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default) =>
        inner.AddAsync(instance, cancellationToken);

    public ValueTask UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default) =>
        inner.UpdateAsync(instance, cancellationToken);

    public ValueTask SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default) =>
        inner.SaveManyAsync(instances, cancellationToken);

    public ValueTask<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        inner.DeleteAsync(filter, cancellationToken);

    public Task UpdateUpdatedTimestampAsync(string workflowInstanceId, DateTimeOffset value, CancellationToken cancellationToken = default) =>
        inner.UpdateUpdatedTimestampAsync(workflowInstanceId, value, cancellationToken);

    public ValueTask<bool> TryMarkInterruptedAsync(string workflowInstanceId, CancellationToken cancellationToken = default, bool allowFinishedCancelled = false) =>
        inner.TryMarkInterruptedAsync(workflowInstanceId, cancellationToken, allowFinishedCancelled);
}
