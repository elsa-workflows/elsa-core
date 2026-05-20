using System.Security.Claims;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

internal static class ImportAuthorizationExtensions
{
    public static async Task<AuthorizationResult> AuthorizeWorkflowDefinitionImportAsync(
        this IAuthorizationService authorizationService,
        ClaimsPrincipal user,
        IWorkflowDefinitionStore workflowDefinitionStore,
        WorkflowDefinitionModel model,
        CancellationToken cancellationToken)
    {
        var definition = await FindExistingDefinitionAsync(workflowDefinitionStore, model.DefinitionId, cancellationToken);
        return await authorizationService.AuthorizeAsync(user, new NotReadOnlyResource(definition), AuthorizationPolicies.NotReadOnlyPolicy);
    }

    public static async Task<AuthorizationResult> AuthorizeWorkflowDefinitionImportsAsync(
        this IAuthorizationService authorizationService,
        ClaimsPrincipal user,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IEnumerable<WorkflowDefinitionModel> models,
        CancellationToken cancellationToken)
    {
        foreach (var model in models)
        {
            var authorizationResult = await authorizationService.AuthorizeWorkflowDefinitionImportAsync(user, workflowDefinitionStore, model, cancellationToken);

            if (!authorizationResult.Succeeded)
                return authorizationResult;
        }

        return await authorizationService.AuthorizeAsync(user, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);
    }

    private static async Task<WorkflowDefinition?> FindExistingDefinitionAsync(
        IWorkflowDefinitionStore workflowDefinitionStore,
        string? definitionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return null;

        return await workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.Latest
        }, cancellationToken);
    }
}
