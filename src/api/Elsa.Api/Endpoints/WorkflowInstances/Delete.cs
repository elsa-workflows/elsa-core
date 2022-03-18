using System.Threading;
using System.Threading.Tasks;
using Elsa.Management.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Requests;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.WorkflowInstances;

public static partial class WorkflowInstances
{
    public static async Task<IResult> DeleteAsync(string id, IRequestSender requestSender,
        IWorkflowInstancePublisher workflowInstancePublisher, CancellationToken cancellationToken)
    {
        var request = new FindWorkflowInstance(id);
        var workflowInstance = await requestSender.RequestAsync(request, cancellationToken);

        if (workflowInstance == null)
        {
            return Results.NotFound();
        }

        await workflowInstancePublisher.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}