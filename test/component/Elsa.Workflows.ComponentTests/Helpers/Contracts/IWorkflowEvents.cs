namespace Elsa.Workflows.ComponentTests;

public interface IWorkflowEvents
{
    event EventHandler<WorkflowFinishedEventArgs> WorkflowFinished;
    event EventHandler<WorkflowInstanceSavedEventArgs> WorkflowInstanceSaved;
    
    void OnWorkflowFinished(WorkflowFinishedEventArgs args);
    void OnWorkflowInstanceSaved(WorkflowInstanceSavedEventArgs args);
}