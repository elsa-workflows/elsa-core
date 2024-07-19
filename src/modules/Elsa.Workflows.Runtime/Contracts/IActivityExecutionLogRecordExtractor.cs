using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// Extracts activity execution log records.
public interface IActivityExecutionRecordExtractor
{
    /// Extracts activity execution logs from a workflow execution context.
    IEnumerable<ActivityExecutionRecord> ExtractWorkflowExecutionLogs(WorkflowExecutionContext context);
}