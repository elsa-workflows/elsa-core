using Elsa.Workflows.Management.Entities;

namespace Elsa.Testing.Shared;

public class WorkflowInstanceSavedEventArgs(WorkflowInstance workflowInstance) : EventArgs 
{
    public WorkflowInstance WorkflowInstance { get; } = workflowInstance;
}