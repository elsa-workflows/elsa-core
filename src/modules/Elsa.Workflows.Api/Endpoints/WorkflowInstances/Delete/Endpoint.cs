using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Persistence.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Delete;

public class Delete : Endpoint<Request>
{
    private readonly IWorkflowInstanceStore _store;
    public Delete(IWorkflowInstanceStore store) => _store = store;

    public override void Configure()
    {
        Delete("/workflow-instances/{id}");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var deleted = await _store.DeleteAsync(request.Id, cancellationToken);

        if (deleted)
            await SendNoContentAsync(cancellationToken);
        else
            await SendNotFoundAsync(cancellationToken);
    }
}