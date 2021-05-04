using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Constructs workflow blueprints from declarative workflow definitions.
    /// </summary>
    public interface IWorkflowBlueprintMaterializer
    {
        Task<IWorkflowBlueprint> CreateWorkflowBlueprintAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
    }
}