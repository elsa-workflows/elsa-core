using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowFactory
    {
        WorkflowBlueprint CreateWorkflowBlueprint(WorkflowDefinitionVersion definition);
        
        Workflow CreateWorkflow<T>(
            Variables input = default, 
            WorkflowInstance workflowInstance = default,
            string correlationId = default)
            where T : IWorkflow, new();

        Workflow CreateWorkflow(
            WorkflowBlueprint blueprint, 
            Variables input = default,
            WorkflowInstance workflowInstance = default,
            string correlationId = default);
    }
}