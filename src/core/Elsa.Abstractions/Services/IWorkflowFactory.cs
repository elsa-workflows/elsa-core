using Elsa.Models;
using Elsa.Serialization.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowFactory
    {
        Workflow CreateWorkflow(WorkflowBlueprint blueprint, Variables input = null, WorkflowInstance workflowInstance = null);
    }
}