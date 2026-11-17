using System.Security.Claims;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

internal static class WorkflowDefinitionMutationAuthorization
{
    public static async Task<bool> CanMutateAsync(
        this IAuthorizationService authorizationService,
        ClaimsPrincipal user,
        IEnumerable<WorkflowDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            var result = await authorizationService.AuthorizeAsync(user, new NotReadOnlyResource(definition), AuthorizationPolicies.NotReadOnlyPolicy);
            if (!result.Succeeded)
                return false;
        }

        return true;
    }

    public static async Task<bool> CanDeleteVersionsAsync(
        this IAuthorizationService authorizationService,
        ClaimsPrincipal user,
        IWorkflowDefinitionStore store,
        ICollection<WorkflowDefinition> versions,
        CancellationToken cancellationToken)
    {
        if (versions.Count == 0)
            return true;

        // The manager marks a surviving version latest only when its flag needs to change.
        var definitionIds = versions.Select(version => version.DefinitionId).Distinct().ToList();
        var deletedIds = versions.Select(version => version.Id).ToHashSet();
        var relatedVersions = await store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionIds = definitionIds }, cancellationToken);
        var survivingLatest = relatedVersions
            .Where(version => !deletedIds.Contains(version.Id))
            .GroupBy(version => version.DefinitionId)
            .Select(group => group.MaxBy(version => version.Version)!)
            .Where(version => !version.IsLatest);

        return await authorizationService.CanMutateAsync(user, versions.Concat(survivingLatest));
    }
}
