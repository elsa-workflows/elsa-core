using Elsa.Workflows.Management.Entities;

namespace Elsa.Testing.Shared.EventArgs;

public class WorkflowInstanceSavedEventArgs(WorkflowInstance workflowInstance) : System.EventArgs 
{
    public WorkflowInstance WorkflowInstance { get; } = workflowInstance;
}