using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Version;

/// <summary>
/// Deletes a specific version of a workflow definition.
/// </summary>
[PublicAPI]
public class DeleteVersion : ElsaEndpointWithoutRequest
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
    private readonly IAuthorizationService _authorizationService;
    private readonly IWorkflowDefinitionStore _store;

    /// <inheritdoc />
    public DeleteVersion(IWorkflowDefinitionManager workflowDefinitionManager, IAuthorizationService authorizationService, IWorkflowDefinitionStore store)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
        _authorizationService = authorizationService;
        _store = store;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("workflow-definitions/{definitionId}/version/{version}");
        ConfigurePermissions("delete:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var definitionId = Route<string>("definitionId")!;
        var version = Route<int>("version");

        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.SpecificVersion(version)
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

        var result = await _workflowDefinitionManager.DeleteVersionAsync(definition, cancellationToken);

        if (!result)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        await SendOkAsync(cancellationToken);
    }
}