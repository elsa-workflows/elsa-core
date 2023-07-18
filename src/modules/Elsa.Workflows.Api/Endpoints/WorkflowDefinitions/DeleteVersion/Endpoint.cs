using Elsa.Abstractions;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.DeleteVersion;

[PublicAPI]
internal class DeleteVersion : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

    public DeleteVersion(IWorkflowDefinitionManager workflowDefinitionManager)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    public override void Configure()
    {
        Delete("/workflow-definition-versions/{id}");
        ConfigurePermissions("delete:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var deleted = await _workflowDefinitionManager.DeleteByIdAsync(request.Id, cancellationToken);

        if (!deleted)
            await SendNotFoundAsync(cancellationToken);
        else
            await SendNoContentAsync(cancellationToken);
    }
}