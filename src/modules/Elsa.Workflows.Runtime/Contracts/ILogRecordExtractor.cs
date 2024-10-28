using Elsa.Common;

namespace Elsa.Workflows.Runtime;

/// Extracts execution log records.
public interface ILogRecordExtractor<T> where T: ILogRecord
{
    /// Extracts execution logs from a workflow execution context.
    Task<IEnumerable<T>> ExtractLogRecordsAsync(WorkflowExecutionContext context);
}