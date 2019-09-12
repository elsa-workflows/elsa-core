using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowFactory
    {
        Workflow CreateWorkflow<T>(Variables input = null, WorkflowInstance workflowInstance = null) where T : IWorkflow, new();
        Workflow CreateWorkflow(WorkflowDefinitionVersion definition, Variables input = null, WorkflowInstance workflowInstance = null);
    }
}