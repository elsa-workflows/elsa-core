using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Delete;

internal class Delete(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService, IWorkflowDefinitionStore store)
    : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/workflow-definitions/{definitionId}");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, CoreVerbs.Delete);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = request.DefinitionId };
        var versions = (await store.FindManyAsync(filter, cancellationToken)).ToList();

        if (!versions.Any(definition => definition.IsLatest))
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        if (!await authorizationService.CanMutateAsync(User, versions))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var result = await workflowDefinitionManager.DeleteByDefinitionIdAsync(request.DefinitionId, cancellationToken);

        if (result == 0)
            await Send.NotFoundAsync(cancellationToken);
        else
            await Send.NoContentAsync(cancellationToken);
    }
}