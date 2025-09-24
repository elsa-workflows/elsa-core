using Elsa.Workflows;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Testing.Shared.EventArgs;

public class ActivityExecutedLogUpdatedEventArgs(WorkflowExecutionContext workflowExecutionContext, ICollection<ActivityExecutionRecord> records) : System.EventArgs
{
    public WorkflowExecutionContext WorkflowExecutionContext { get; } = workflowExecutionContext;
    public ICollection<ActivityExecutionRecord> Records { get; } = records;
}