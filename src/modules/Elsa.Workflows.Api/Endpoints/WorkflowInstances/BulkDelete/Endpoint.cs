using Elsa.Abstractions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkDelete;

[PublicAPI]
internal class BulkDelete(IWorkflowInstanceManager store) : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
            "/bulk-actions/delete/workflow-instances",
            "/bulk-actions/delete/workflow-instances/by-id" // Deprecated route.
        );
        ConfigurePermissions("delete:workflow-instances");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter
        {
            Ids = request.Ids,
            DefinitionId = request.WorkflowDefinitionId,
            DefinitionIds = request.WorkflowDefinitionIds,
        };
        var count = await store.BulkDeleteAsync(filter, cancellationToken);

        return new(count);   
    }
}