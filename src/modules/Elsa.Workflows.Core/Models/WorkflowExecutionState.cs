namespace Elsa.Workflows;

internal class WorkflowExecutionState : IDisposable
{
    private readonly ActivityExecutionContext _activityExecutionContext;

    public WorkflowExecutionState(ActivityExecutionContext activityExecutionContext)
    {
        _activityExecutionContext = activityExecutionContext;
        _activityExecutionContext.IsExecuting = activityExecutionContext.WorkflowExecutionContext.IsExecuting = true;
    }

    public void Dispose()
    {
        _activityExecutionContext.IsExecuting = _activityExecutionContext.WorkflowExecutionContext.IsExecuting = false;
    }
}