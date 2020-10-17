using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowBlueprintMaterializer
    {
        IWorkflowBlueprint CreateWorkflowBlueprint(WorkflowDefinition workflowDefinition);
    }
}