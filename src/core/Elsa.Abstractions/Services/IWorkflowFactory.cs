using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowFactory
    {
        WorkflowBlueprint CreateWorkflowBlueprint(WorkflowDefinitionVersion definition);
        
        Workflow CreateWorkflow<T>(
            Variable? input = default, 
            WorkflowInstance? workflowInstance = default,
            string correlationId = default)
            where T : IWorkflow, new();

        Workflow CreateWorkflow(
            WorkflowBlueprint blueprint, 
            Variable? input = default,
            WorkflowInstance? workflowInstance = default,
            string correlationId = default);
    }
}