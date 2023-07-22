using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// A workflow execution log store that does nothing.
/// </summary>
public class NoopWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    /// <inheritdoc />
    public Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(default(WorkflowExecutionLogRecord?));

    /// <inheritdoc />
    public Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default) => Task.FromResult(default(WorkflowExecutionLogRecord?));

    /// <inheritdoc />
    public Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default) => Task.FromResult(Page.Of(new List<WorkflowExecutionLogRecord>(0), 0));

    /// <inheritdoc />
    public Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default) => Task.FromResult(Page.Of(new List<WorkflowExecutionLogRecord>(0), 0));

    /// <inheritdoc />
    public Task<long> DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(0L);
}