using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Services
{
    public interface IWorkflowBlueprintMapper
    {
        ValueTask<WorkflowBlueprintModel> MapAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default);
    }
}