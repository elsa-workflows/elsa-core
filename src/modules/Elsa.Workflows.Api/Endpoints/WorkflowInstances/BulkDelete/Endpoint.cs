using Elsa.Abstractions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkDelete;

[PublicAPI]
internal class BulkDelete : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowInstanceManager _store;

    public BulkDelete(IWorkflowInstanceManager store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Post("/bulk-actions/delete/workflow-instances/by-id");
        ConfigurePermissions("delete:workflow-instances");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter { Ids = request.Ids };
        var count = await _store.BulkDeleteAsync(filter, cancellationToken);

        return new Response(count);   
    }
}