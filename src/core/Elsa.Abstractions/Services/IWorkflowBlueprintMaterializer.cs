using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Constructs workflow blueprints from declarative workflow definitions.
    /// </summary>
    public interface IWorkflowBlueprintMaterializer
    {
        IWorkflowBlueprint CreateWorkflowBlueprint(WorkflowDefinition workflowDefinition);
    }
}