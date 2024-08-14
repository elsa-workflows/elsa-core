using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.DeleteVersion;

[PublicAPI]
internal class DeleteVersion(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/workflow-definition-versions/{id}");
        ConfigurePermissions("delete:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var authorizationResult = authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Result.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return;
        }

        var deleted = await workflowDefinitionManager.DeleteByIdAsync(request.Id, cancellationToken);

        if (!deleted)
            await SendNotFoundAsync(cancellationToken);
        else
            await SendNoContentAsync(cancellationToken);
    }
}