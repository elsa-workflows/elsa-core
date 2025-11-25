using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Delete;

[PublicAPI]
internal class Delete(IWorkflowRuntime workflowRuntime) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/workflow-instances/{id}");
        ConfigurePermissions("delete:workflow-instances");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var client = await workflowRuntime.CreateClientAsync(request.Id, cancellationToken);
        var deleted = await client.DeleteAsync(cancellationToken);

        if (deleted)
            await Send.NoContentAsync(cancellationToken);
        else
            await Send.NotFoundAsync(cancellationToken);
    }
}