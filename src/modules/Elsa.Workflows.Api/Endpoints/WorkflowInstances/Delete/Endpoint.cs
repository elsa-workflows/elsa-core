using Elsa.Abstractions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Delete;

[PublicAPI]
internal class Delete : ElsaEndpoint<Request>
{
    private readonly IWorkflowInstanceManager _store;
    public Delete(IWorkflowInstanceManager store) => _store = store;

    public override void Configure()
    {
        Delete("/workflow-instances/{id}");
        ConfigurePermissions("delete:workflow-instances");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter { Id = request.Id };
        var deleted = await _store.DeleteAsync(filter, cancellationToken);

        if (deleted)
            await SendNoContentAsync(cancellationToken);
        else
            await SendNotFoundAsync(cancellationToken);
    }
}