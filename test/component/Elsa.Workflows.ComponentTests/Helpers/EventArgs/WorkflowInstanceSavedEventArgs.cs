using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.ComponentTests;

public class WorkflowInstanceSavedEventArgs(WorkflowInstance workflowInstance) : EventArgs 
{
    public WorkflowInstance WorkflowInstance { get; } = workflowInstance;
}