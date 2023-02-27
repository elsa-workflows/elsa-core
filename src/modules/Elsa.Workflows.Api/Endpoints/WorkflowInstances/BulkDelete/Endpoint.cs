using Elsa.Abstractions;
using Elsa.Workflows.Management.Services;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkDelete;

[PublicAPI]
internal class BulkDelete : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowInstanceStore _store;

    public BulkDelete(IWorkflowInstanceStore store)
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
        var count = await _store.DeleteManyAsync(filter, cancellationToken);

        return new Response(count);   
    }
}