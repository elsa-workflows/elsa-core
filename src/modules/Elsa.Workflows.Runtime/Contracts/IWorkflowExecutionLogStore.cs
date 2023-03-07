using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents a store of <see cref="WorkflowExecutionLogRecord"/>.
/// </summary>
public interface IWorkflowExecutionLogStore
{
    /// <summary>
    /// Save the specified <see cref="WorkflowExecutionLogRecord"/> to te persistence store.
    /// </summary>
    Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Save the specified <see cref="WorkflowExecutionLogRecord"/> to te persistence store.
    /// </summary>
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
}

/// <summary>
/// Represents the order by which to order the results of a query.
/// </summary>
public class WorkflowExecutionLogRecordOrder<TProp> : OrderDefinition<WorkflowExecutionLogRecord, TProp>
{
    /// <summary>
    /// Creates a new instance of the <see cref="WorkflowExecutionLogRecordOrder{TProp}"/> class.
    /// </summary>
    public WorkflowExecutionLogRecordOrder(Expression<Func<WorkflowExecutionLogRecord, TProp>> keySelector, OrderDirection direction)
    {
        KeySelector = keySelector;
        Direction = direction;
    }
}

/// <summary>
/// A specification to use when finding workflow execution log records. Only non-null fields will be included in the conditional expression.
/// </summary>
public class WorkflowExecutionLogRecordFilter
{
    /// <summary>
    /// The ID of the workflow instance.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// The ID of the activity.
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// The name of the event.
    /// </summary>
    public string? EventName { get; set; }
    
    /// <summary>
    /// Match any of these event names.
    /// </summary>
    public ICollection<string>? AnyEventName { get; set; }
    
    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    public IQueryable<WorkflowExecutionLogRecord> Apply(IQueryable<WorkflowExecutionLogRecord> queryable)
    {
        var filter = this;
        if (filter.WorkflowInstanceId != null) queryable = queryable.Where(x => x.WorkflowInstanceId == filter.WorkflowInstanceId);
        if (filter.ActivityId != null) queryable = queryable.Where(x => x.ActivityId == filter.ActivityId);
        if (filter.EventName != null) queryable = queryable.Where(x => x.EventName == filter.EventName);
        if (filter.AnyEventName != null) queryable = queryable.Where(x => filter.AnyEventName.Contains(x.EventName!));
        
        return queryable;
    }
}