using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

public static partial class WorkflowDefinitions
{
    public static async Task<IResult> DeleteAsync(string definitionId, IWorkflowDefinitionStore workflowDefinitionStore, CancellationToken cancellationToken)
    {
        var result = await workflowDefinitionStore.DeleteByDefinitionIdAsync(definitionId, cancellationToken);
        return result == 0 ? Results.NotFound() : Results.NoContent();
    }
}