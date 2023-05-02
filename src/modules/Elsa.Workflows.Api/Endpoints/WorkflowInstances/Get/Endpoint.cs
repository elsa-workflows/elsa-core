using Elsa.Abstractions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Get;

[PublicAPI]
internal class Get : ElsaEndpoint<Request, Response, WorkflowInstanceMapper>
{
    private readonly IWorkflowInstanceStore _store;

    public Get(IWorkflowInstanceStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/workflow-instances/{id}");
        ConfigurePermissions("read:workflow-instances");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter { Id = request.Id };
        var workflowInstance = await _store.FindAsync(filter, cancellationToken);

        if (workflowInstance == null)
            await SendNotFoundAsync(cancellationToken);
        else
            await SendOkAsync(Map.FromEntity(workflowInstance), cancellationToken);
    }
}