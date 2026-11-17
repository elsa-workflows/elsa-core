using System.Security.Claims;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
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

    public static async Task<WorkflowDefinitionDeletionPlan> PlanDeletionAsync(
        this IWorkflowDefinitionStore store,
        ICollection<WorkflowDefinition> versions,
        CancellationToken cancellationToken)
    {
        if (versions.Count == 0)
            return new(versions, versions);

        var definitionIds = versions.Select(version => version.DefinitionId).Distinct().ToList();
        var relatedVersions = await store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionIds = definitionIds }, cancellationToken);
        return new(versions, relatedVersions);
    }

    public static Task<bool> CanDeleteAsync(this IAuthorizationService authorizationService, ClaimsPrincipal user, WorkflowDefinitionDeletionPlan plan) =>
        authorizationService.CanMutateAsync(user, plan.Deleted.Concat(plan.Promoted));
}
