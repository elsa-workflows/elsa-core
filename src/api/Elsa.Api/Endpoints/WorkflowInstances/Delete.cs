using System.Threading;
using System.Threading.Tasks;
using Elsa.Management.Services;
using Elsa.Mediator.Services;
using Elsa.Persistence.Requests;
using Elsa.Persistence.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.WorkflowInstances;

public static partial class WorkflowInstances
{
    public static async Task<IResult> DeleteAsync(
        string id, 
        IWorkflowInstanceStore workflowInstanceStore,
        CancellationToken cancellationToken)
    {
        var workflowInstance = await workflowInstanceStore.FindByIdAsync(id, cancellationToken);

        if (workflowInstance == null)
            return Results.NotFound();

        await workflowInstanceStore.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}