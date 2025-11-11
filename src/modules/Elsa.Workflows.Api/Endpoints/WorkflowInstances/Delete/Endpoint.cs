using Elsa.Abstractions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Delete;

[PublicAPI]
internal class Delete(IWorkflowInstanceManager store) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/workflow-instances/{id}");
        ConfigurePermissions("delete:workflow-instances");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter { Id = request.Id };
        var deleted = await store.DeleteAsync(filter, cancellationToken);

        if (deleted)
            await Send.NoContentAsync(cancellationToken);
        else
            await Send.NotFoundAsync(cancellationToken);
    }
}