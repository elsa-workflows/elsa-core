using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Persistence.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Get;

public class Get : Endpoint<Request, Response, WorkflowInstanceMapper>
{
    private readonly IWorkflowInstanceStore _store;

    public Get(IWorkflowInstanceStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/workflow-instances/{id}");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowInstance = await _store.FindByIdAsync(request.Id, cancellationToken);

        if (workflowInstance == null)
            await SendNotFoundAsync(cancellationToken);
        else
            await SendOkAsync(await Map.FromEntityAsync(workflowInstance), cancellationToken);
    }
}