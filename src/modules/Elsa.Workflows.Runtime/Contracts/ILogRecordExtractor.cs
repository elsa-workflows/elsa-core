using Elsa.Common;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Extracts execution log records.
/// </summary>
public interface ILogRecordExtractor<T> where T: ILogRecord
{
    /// <summary>
    /// Extracts execution logs from a workflow execution context.
    /// </summary>
    Task<IEnumerable<T>> ExtractLogRecordsAsync(WorkflowExecutionContext context);
}