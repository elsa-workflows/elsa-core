using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.UpdateReferences;

[PublicAPI]
internal class UpdateReferences : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
    private readonly IAuthorizationService _authorizationService;

    public UpdateReferences(IWorkflowDefinitionStore store, IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService)
    {
        _store = store;
        _workflowDefinitionManager = workflowDefinitionManager;
        _authorizationService = authorizationService;
    }

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

        var definition = await _store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var authorizationResult = _authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(definition), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Result.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return;
        }

        var affectedWorkflows = await _workflowDefinitionManager.UpdateReferencesInConsumingWorkflows(definition, cancellationToken);
        var response = new Response(affectedWorkflows.Select(w => w.Name ?? w.DefinitionId));
        await SendOkAsync(response, cancellationToken);
    }
}