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
        var modelList = models.ToList();

        if (modelList.Count == 0)
            return await authorizationService.AuthorizeAsync(user, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        var definitions = await FindExistingDefinitionsAsync(workflowDefinitionStore, modelList, cancellationToken);

        foreach (var model in modelList)
        {
            definitions.TryGetValue(model.DefinitionId ?? string.Empty, out var definition);
            var authorizationResult = await authorizationService.AuthorizeAsync(user, new NotReadOnlyResource(definition), AuthorizationPolicies.NotReadOnlyPolicy);

            if (!authorizationResult.Succeeded)
                return authorizationResult;
        }

        return AuthorizationResult.Success();
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

    private static async Task<IDictionary<string, WorkflowDefinition>> FindExistingDefinitionsAsync(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IEnumerable<WorkflowDefinitionModel> models,
        CancellationToken cancellationToken)
    {
        var definitionIds = models
            .Select(x => x.DefinitionId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (definitionIds.Count == 0)
            return new Dictionary<string, WorkflowDefinition>();

        var definitions = await workflowDefinitionStore.FindManyAsync(new WorkflowDefinitionFilter
        {
            DefinitionIds = definitionIds,
            VersionOptions = VersionOptions.Latest
        }, cancellationToken);

        return definitions
            .GroupBy(x => x.DefinitionId, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
    }
}
