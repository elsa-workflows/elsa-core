using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDeleteVersions;

[PublicAPI]
internal class BulkDeleteVersions(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/bulk-actions/delete/workflow-definitions/by-id");
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

        var count = await workflowDefinitionManager.BulkDeleteByIdsAsync(request.Ids, cancellationToken);
        return new Response(count);
    }
}