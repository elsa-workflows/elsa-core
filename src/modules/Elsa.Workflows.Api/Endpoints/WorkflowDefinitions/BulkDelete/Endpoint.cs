using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDelete;

[UsedImplicitly]
internal class BulkDelete : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

    public BulkDelete(IWorkflowDefinitionManager workflowDefinitionManager)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    public override void Configure()
    {
        Post("/bulk-actions/delete/workflow-definitions/by-definition-id");
        ConfigurePermissions("delete:workflow-definitions");
        Policies(AuthorizationPolicies.NotReadOnlyPolicy);
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var count = await _workflowDefinitionManager.BulkDeleteByDefinitionIdsAsync(request.DefinitionIds, cancellationToken);
        return new Response(count);
    }
}