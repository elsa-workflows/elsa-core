using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// Extracts workflow execution log records.
public interface IWorkflowExecutionLogRecordExtractor
{
    /// Extracts workflow execution logs from a workflow execution context.
    IEnumerable<WorkflowExecutionLogRecord> ExtractWorkflowExecutionLogs(WorkflowExecutionContext context);
}