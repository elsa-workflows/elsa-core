namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowDispatchOutboxAccessor : IWorkflowDispatchOutboxAccessor
{
    private readonly AsyncLocal<WorkflowExecutionContext?> _currentWorkflowExecutionContext = new();

    /// <inheritdoc />
    public WorkflowExecutionContext? WorkflowExecutionContext
    {
        get => _currentWorkflowExecutionContext.Value;
        set => _currentWorkflowExecutionContext.Value = value;
    }
}
