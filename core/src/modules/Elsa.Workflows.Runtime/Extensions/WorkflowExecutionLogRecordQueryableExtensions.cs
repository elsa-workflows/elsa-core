using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Canonical default order for <see cref="IWorkflowExecutionLogStore"/> queries that omit an explicit order.
/// </summary>
public static class WorkflowExecutionLogRecordQueryableExtensions
{
    /// <summary>
    /// Timestamp ascending, then Sequence ascending, then Id ascending.
    /// </summary>
    /// <remarks>
    /// Preserves EF's historical Timestamp default and uses Sequence as the same-timestamp
    /// tiebreaker — the purpose of <see cref="WorkflowExecutionLogRecord.Sequence"/>.
    /// Sequence is per execution context, so distinct instances can share Timestamp+Sequence;
    /// Id is the unique key that keeps offset pagination stable across Memory and EF.
    /// Journal list APIs still pass Sequence-primary order explicitly because Sequence is
    /// the instance-monotonic event cursor (<c>ExecutionLogSequence</c>).
    /// </remarks>
    public static IQueryable<WorkflowExecutionLogRecord> OrderByTimestampThenSequence(this IQueryable<WorkflowExecutionLogRecord> queryable) =>
        queryable.OrderBy(x => x.Timestamp).ThenBy(x => x.Sequence).ThenBy(x => x.Id);
}
