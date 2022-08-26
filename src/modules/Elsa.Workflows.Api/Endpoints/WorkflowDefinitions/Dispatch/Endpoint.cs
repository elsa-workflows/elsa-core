using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Dispatch;

public class Endpoint : Endpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDispatcher _workflowDispatcher;

    public Endpoint(IWorkflowDefinitionStore store, IWorkflowDispatcher workflowDispatcher)
    {
        _store = store;
        _workflowDispatcher = workflowDispatcher;
    }

    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/dispatch");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var exists = await _store.GetExistsAsync(request.DefinitionId, VersionOptions.Published, cancellationToken);

        if (!exists)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var result = await _workflowDispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest(
                request.DefinitionId,
                VersionOptions.Published,
                CorrelationId: request.CorrelationId),
            cancellationToken);

        await SendOkAsync(new Response(), cancellationToken);
    }
}