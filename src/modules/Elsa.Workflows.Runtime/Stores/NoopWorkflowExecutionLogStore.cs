using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime.Stores;

public class NoopWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    public Task AddAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task AddManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<WorkflowExecutionLogRecord?>(null);
    }

    public Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<WorkflowExecutionLogRecord?>(null);
    }

    public Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Page<WorkflowExecutionLogRecord>(new List<WorkflowExecutionLogRecord>(0), 0));
    }

    public Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Page<WorkflowExecutionLogRecord>(new List<WorkflowExecutionLogRecord>(0), 0));
    }

    public Task<long> DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0L);
    }
}