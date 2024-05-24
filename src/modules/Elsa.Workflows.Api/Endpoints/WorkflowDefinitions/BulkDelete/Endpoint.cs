using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDelete;

[UsedImplicitly]
internal class BulkDelete : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
    private readonly IAuthorizationService _authorizationService;

    public BulkDelete(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
        _authorizationService = authorizationService;
    }

    public override void Configure()
    {
        Post("/bulk-actions/delete/workflow-definitions/by-definition-id");
        ConfigurePermissions("delete:workflow-definitions");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var authorizationResult = _authorizationService.AuthorizeAsync(User, null, AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Result.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return null!;
        }

        var count = await _workflowDefinitionManager.BulkDeleteByDefinitionIdsAsync(request.DefinitionIds, cancellationToken);
        return new Response(count);
    }
}