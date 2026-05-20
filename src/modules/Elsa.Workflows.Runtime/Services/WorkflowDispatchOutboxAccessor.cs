namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowDispatchOutboxAccessor : IWorkflowDispatchOutboxAccessor
{
    private static readonly AsyncLocal<WorkflowExecutionContext?> CurrentWorkflowExecutionContext = new();

    /// <inheritdoc />
    public WorkflowExecutionContext? WorkflowExecutionContext
    {
        get => CurrentWorkflowExecutionContext.Value;
        set => CurrentWorkflowExecutionContext.Value = value;
    }
}
