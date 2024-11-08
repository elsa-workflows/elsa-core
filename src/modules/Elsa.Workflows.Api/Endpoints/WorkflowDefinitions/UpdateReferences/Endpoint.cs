using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.UpdateReferences;

[PublicAPI]
internal class UpdateReferences(IWorkflowReferenceUpdater workflowReferenceUpdater, IWorkflowDefinitionStore store, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/update-references");
        ConfigurePermissions("publish:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = VersionOptions.Latest
        };

        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var authorizationResult = authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(definition), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Result.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return;
        }

        var result = await workflowReferenceUpdater.UpdateWorkflowReferencesAsync(definition, cancellationToken);
        var affectedWorkflows = result.UpdatedWorkflows;
        var response = new Response(affectedWorkflows.Select(w => w.Name ?? w.DefinitionId));
        await SendOkAsync(response, cancellationToken);
    }
}