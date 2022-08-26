using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

public class Execute : Endpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowInvoker _workflowInvoker;

    public Execute(IWorkflowDefinitionStore store, IWorkflowInvoker workflowInvoker)
    {
        _store = store;
        _workflowInvoker = workflowInvoker;
    }

    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/execute");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var exists = await _store.GetExistsAsync(request.DefinitionId, VersionOptions.Published, cancellationToken);

        if (!exists)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var executeRequest = new InvokeWorkflowDefinitionRequest(request.DefinitionId, VersionOptions.Published, CorrelationId: request.CorrelationId);
        var result = await _workflowInvoker.InvokeAsync(executeRequest, CancellationToken.None);

        if (!HttpContext.Response.HasStarted)
            await SendOkAsync(new Response(result.WorkflowState, result.Bookmarks), cancellationToken);
    }
}