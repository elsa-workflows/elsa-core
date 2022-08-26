using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Management.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Delete;

public class Delete : Endpoint<Request>
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

    public Delete(IWorkflowDefinitionManager workflowDefinitionManager)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    public override void Configure()
    {
        Delete("/workflow-definitions/{definitionId}");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await _workflowDefinitionManager.DeleteByDefinitionIdAsync(request.DefinitionId, cancellationToken);

        if (result == 0)
            await SendNotFoundAsync(cancellationToken);
        else
            await SendNoContentAsync(cancellationToken);
    }
}