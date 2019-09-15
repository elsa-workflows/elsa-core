using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowFactory
    {
        Workflow CreateWorkflow<T>(
            Variables input = default, 
            WorkflowInstance workflowInstance = default,
            string correlationId = default)
            where T : IWorkflow, new();

        Workflow CreateWorkflow(
            WorkflowDefinitionVersion definition, 
            Variables input = default,
            WorkflowInstance workflowInstance = default,
            string correlationId = default);
    }
}