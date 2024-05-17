namespace Elsa.Workflows.ComponentTests;

public interface IWorkflowDefinitionEvents
{
    event EventHandler<WorkflowDefinitionDeletedEventArgs> WorkflowDefinitionDeleted;
    
    void OnWorkflowDefinitionDeleted(WorkflowDefinitionDeletedEventArgs args);
}