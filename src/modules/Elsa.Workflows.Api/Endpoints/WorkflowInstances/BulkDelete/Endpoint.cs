using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions;
using Elsa.Workflows.Persistence.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkDelete;

public class BulkDelete : ElsaEndpoint<Request, Response>
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
        var count = await _store.DeleteManyAsync(request.Ids, cancellationToken);

        return new Response(count);   
    }
}