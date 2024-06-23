using Elsa.Abstractions;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Get;

[PublicAPI]
internal class Get(IWorkflowInstanceStore store) : ElsaEndpoint<Request, WorkflowInstanceModel, WorkflowInstanceMapper>
{
    public override void Configure()
    {
        Get("/workflow-instances/{id}");
        ConfigurePermissions("read:workflow-instances");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter { Id = request.Id };
        var workflowInstance = await store.FindAsync(filter, cancellationToken);

        if (workflowInstance == null)
            await SendNotFoundAsync(cancellationToken);
        else
            await SendOkAsync(Map.FromEntity(workflowInstance), cancellationToken);
    }
}