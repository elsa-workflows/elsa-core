using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Elsa.Workflows.Api.Requirements;
namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDelete;

[UsedImplicitly]
internal class BulkDelete(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/bulk-actions/delete/workflow-definitions/by-definition-id");
        ConfigurePermissions("delete:workflow-definitions");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var authorizationResult = authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Result.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return null!;
        }

        var count = await workflowDefinitionManager.BulkDeleteByDefinitionIdsAsync(request.DefinitionIds, cancellationToken);
        return new Response(count);
    }
}