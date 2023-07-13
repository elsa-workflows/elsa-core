using Elsa.Abstractions;
using Elsa.Workflows.Management.Contracts;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Delete;

internal class Delete : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

    public Delete(IWorkflowDefinitionManager workflowDefinitionManager)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    public override void Configure()
    {
        Delete("/workflow-definitions/{definitionId}");
        ConfigurePermissions("delete:workflow-definitions");
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