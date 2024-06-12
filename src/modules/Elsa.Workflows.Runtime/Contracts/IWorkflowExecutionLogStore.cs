using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a store of <see cref="WorkflowExecutionLogRecord"/>.
/// </summary>
public interface IWorkflowExecutionLogStore
{
    /// <summary>
    /// Adds the specified <see cref="WorkflowExecutionLogRecord"/> to te persistence store.
    /// </summary>
    Task AddAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the specified set of <see cref="WorkflowExecutionLogRecord"/> objects to te persistence store.
    /// </summary>
    Task AddManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates the specified <see cref="WorkflowExecutionLogRecord"/> in the persistence store.
    /// </summary>
    /// <remarks>
    /// If the record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates the specified set of <see cref="WorkflowExecutionLogRecord"/> objects in the persistence store.
    /// </summary>
    /// <remarks>
    /// If a record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first workflow execution log record matching the specified filter.
    /// </summary>
    Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first workflow execution log record matching the specified filter.
    /// </summary>
    Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a set of workflow execution log records matching the specified filter.
    /// </summary>
    Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a set of workflow execution log records matching the specified filter.
    /// </summary>
    Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all workflow execution log records matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The number of deleted records.</returns>
    Task<long> DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default);
}