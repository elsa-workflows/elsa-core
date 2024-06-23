namespace Elsa.Testing.Shared;

public interface IWorkflowDefinitionEvents
{
    event EventHandler<WorkflowDefinitionDeletedEventArgs> WorkflowDefinitionDeleted;
    
    void OnWorkflowDefinitionDeleted(WorkflowDefinitionDeletedEventArgs args);
}