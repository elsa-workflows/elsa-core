using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Elsa.Authorization;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <inheritdoc />
public sealed class RoleDeletionCoordinator(
    IRoleStore roleStore,
    IRoleAuthorizationService roleAuthorizationService,
    IEnumerable<IRoleDeletionDependencyContributor> contributors) : IRoleDeletionCoordinator
{
    private readonly IReadOnlyDictionary<string, IRoleDeletionDependencyContributor> _contributors = contributors.ToDictionary(x => x.Source, StringComparer.Ordinal);

    /// <inheritdoc />
    public async ValueTask<RoleDeletionInspectionResult> InspectAsync(string roleId, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var role = await roleStore.FindAsync(new() { Id = roleId }, cancellationToken);
        if (role is null)
            return new RoleDeletionInspectionResult.NotFound();
        if (!HasPermission(actor) || !roleAuthorizationService.CanMutateRole(actor, role))
            return new RoleDeletionInspectionResult.Forbidden();

        var snapshots = await InspectContributorsAsync(roleId, cancellationToken);
        return new RoleDeletionInspectionResult.Success(CreateImpact(roleId, snapshots));
    }

    /// <inheritdoc />
    public async ValueTask<RoleDeletionOperationResult> DeleteAsync(string roleId, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var inspection = await InspectAsync(roleId, actor, cancellationToken);
        if (inspection is RoleDeletionInspectionResult.NotFound)
            return new RoleDeletionOperationResult.NotFound();
        if (inspection is RoleDeletionInspectionResult.Forbidden)
            return new RoleDeletionOperationResult.Forbidden();

        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;
        if (!impact.CanDelete)
            return new RoleDeletionOperationResult.Blocked(impact);

        await roleStore.DeleteAsync(new() { Id = roleId }, cancellationToken);
        return new RoleDeletionOperationResult.Deleted([]);
    }

    /// <inheritdoc />
    public async ValueTask<RoleDeletionOperationResult> RemediateAndDeleteAsync(RoleDeletionRemediationCommand command, CancellationToken cancellationToken = default)
    {
        var inspection = await InspectAsync(command.RoleId, command.Actor, cancellationToken);
        if (inspection is RoleDeletionInspectionResult.NotFound)
            return new RoleDeletionOperationResult.NotFound();
        if (inspection is RoleDeletionInspectionResult.Forbidden)
            return new RoleDeletionOperationResult.Forbidden();

        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;
        if (!string.Equals(impact.DependencyVersion, command.ExpectedDependencyVersion, StringComparison.Ordinal))
            return new RoleDeletionOperationResult.PreconditionFailed(impact);
        if (impact.Dependencies.Any(x => x.Ownership == RoleDeletionDependencyOwnership.Configuration))
            return new RoleDeletionOperationResult.Blocked(impact);
        if (impact.CanDelete)
        {
            await roleStore.DeleteAsync(new() { Id = command.RoleId }, cancellationToken);
            return new RoleDeletionOperationResult.Deleted([]);
        }

        var warnings = GetRequiredConfirmations(impact, command);
        if (warnings.Count != 0)
            return new RoleDeletionOperationResult.ConfirmationRequired(impact, warnings);

        var snapshots = await InspectContributorsAsync(command.RoleId, cancellationToken);
        var currentImpact = CreateImpact(command.RoleId, snapshots);
        if (!string.Equals(currentImpact.DependencyVersion, command.ExpectedDependencyVersion, StringComparison.Ordinal))
            return new RoleDeletionOperationResult.PreconditionFailed(currentImpact);

        var requests = snapshots
            .Where(x => x.Dependencies.Any(dependency => dependency.Ownership == RoleDeletionDependencyOwnership.Database))
            .Select(snapshot => new RoleReferenceRemovalRequest(
                command.RoleId,
                command.Actor,
                snapshot.Version,
                snapshot.Dependencies.Where(x => x.Ownership == RoleDeletionDependencyOwnership.Database).ToArray()))
            .ToArray();

        foreach (var request in requests)
        {
            var validation = await _contributors[request.Dependencies.First().Source].ValidateRemovalAsync(request, cancellationToken);
            if (validation is RoleReferenceRemovalValidationResult.Forbidden)
                return new RoleDeletionOperationResult.Forbidden();
            if (validation is RoleReferenceRemovalValidationResult.Conflict)
                return new RoleDeletionOperationResult.PreconditionFailed(await GetCurrentImpactAsync(command.RoleId, cancellationToken));
        }

        var changedOwnerIds = new List<string>();
        foreach (var request in requests)
        {
            var removal = await _contributors[request.Dependencies.First().Source].RemoveEditableReferencesAsync(request, cancellationToken);
            switch (removal)
            {
                case RoleReferenceRemovalResult.Success success:
                    changedOwnerIds.AddRange(success.ChangedOwnerIds);
                    break;
                case RoleReferenceRemovalResult.Conflict conflict:
                    changedOwnerIds.AddRange(conflict.ChangedOwnerIds);
                    return new RoleDeletionOperationResult.Incomplete(await GetCurrentImpactAsync(command.RoleId, cancellationToken), changedOwnerIds.Distinct(StringComparer.Ordinal).ToArray(), conflict.Code);
                case RoleReferenceRemovalResult.Failed failed:
                    changedOwnerIds.AddRange(failed.ChangedOwnerIds);
                    return new RoleDeletionOperationResult.Incomplete(await GetCurrentImpactAsync(command.RoleId, cancellationToken), changedOwnerIds.Distinct(StringComparer.Ordinal).ToArray(), failed.Code);
            }
        }

        var finalInspection = await InspectAsync(command.RoleId, command.Actor, cancellationToken);
        if (finalInspection is RoleDeletionInspectionResult.NotFound)
            return new RoleDeletionOperationResult.NotFound();
        if (finalInspection is RoleDeletionInspectionResult.Forbidden)
            return new RoleDeletionOperationResult.Forbidden();

        var finalImpact = ((RoleDeletionInspectionResult.Success)finalInspection).Impact;
        if (!finalImpact.CanDelete)
            return new RoleDeletionOperationResult.Incomplete(finalImpact, changedOwnerIds.Distinct(StringComparer.Ordinal).ToArray(), "role_dependencies_remain");

        await roleStore.DeleteAsync(new() { Id = command.RoleId }, cancellationToken);
        return new RoleDeletionOperationResult.Deleted(changedOwnerIds.Distinct(StringComparer.Ordinal).ToArray());
    }

    private async ValueTask<IReadOnlyCollection<RoleDeletionDependencySnapshot>> InspectContributorsAsync(string roleId, CancellationToken cancellationToken)
    {
        var snapshots = new List<RoleDeletionDependencySnapshot>(_contributors.Count);
        foreach (var contributor in _contributors.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Value))
        {
            var snapshot = await contributor.InspectAsync(roleId, cancellationToken);
            if (!string.Equals(snapshot.Source, contributor.Source, StringComparison.Ordinal) ||
                snapshot.Dependencies.Any(x => !string.Equals(x.Source, contributor.Source, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Role-deletion contributor '{contributor.Source}' returned a mismatched source identifier.");
            snapshots.Add(snapshot);
        }

        return snapshots;
    }

    private static RoleDeletionImpact CreateImpact(string roleId, IReadOnlyCollection<RoleDeletionDependencySnapshot> snapshots)
    {
        var dependencies = snapshots
            .SelectMany(x => x.Dependencies)
            .OrderBy(x => x.Source, StringComparer.Ordinal)
            .ThenBy(x => x.Ownership)
            .ThenBy(x => x.OwnerId, StringComparer.Ordinal)
            .ThenBy(x => x.PolicyBranch, StringComparer.Ordinal)
            .ToArray();
        // The current coordinator has no unit of work spanning contributor stores and IRoleStore.
        // Contributor-local atomicity alone cannot make the complete remove-then-delete command atomic.
        var hasEditableDependencies = snapshots.Any(x => x.Dependencies.Any(dependency => dependency.Ownership == RoleDeletionDependencyOwnership.Database));
        var executionMode = hasEditableDependencies ? RoleDeletionExecutionMode.BestEffort : RoleDeletionExecutionMode.Atomic;
        return new RoleDeletionImpact(
            roleId,
            CalculateDependencyVersion(snapshots),
            executionMode,
            dependencies.Length == 0,
            dependencies.Length != 0 && dependencies.All(x => x.Ownership == RoleDeletionDependencyOwnership.Database),
            dependencies);
    }

    private static string CalculateDependencyVersion(IEnumerable<RoleDeletionDependencySnapshot> snapshots)
    {
        var payload = string.Join(
            "\n",
            snapshots
                .OrderBy(x => x.Source, StringComparer.Ordinal)
                .SelectMany(snapshot => new[] { $"{snapshot.Source}|{snapshot.Version}|{snapshot.SupportsAtomicRemoval}" }
                    .Concat(snapshot.Dependencies
                        .OrderBy(x => x.Ownership)
                        .ThenBy(x => x.OwnerId, StringComparer.Ordinal)
                        .ThenBy(x => x.PolicyBranch, StringComparer.Ordinal)
                        .Select(x => $"{x.Source}|{x.OwnerId}|{x.OwnerKey}|{x.PolicyBranch}|{x.Ownership}|{x.ConfigurationPath}|{x.ExpectedRevision}|{x.RemovesLastDefaultRole}"))));
        return $"role-dependencies-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant()}";
    }

    private static IReadOnlyCollection<string> GetRequiredConfirmations(RoleDeletionImpact impact, RoleDeletionRemediationCommand command)
    {
        var warnings = new List<string>();
        if (!command.ConfirmRemoveFromEditablePolicies)
            warnings.Add("confirm_remove_from_editable_jit_policies");
        if (impact.Dependencies.Any(x => x.RemovesLastDefaultRole) && !command.ConfirmEmptyDefaultRoles)
            warnings.Add("removes_last_default_role");
        if (impact.ExecutionMode == RoleDeletionExecutionMode.BestEffort && !command.ConfirmBestEffort)
            warnings.Add("confirm_best_effort");
        return warnings;
    }

    private async ValueTask<RoleDeletionImpact> GetCurrentImpactAsync(string roleId, CancellationToken cancellationToken) =>
        CreateImpact(roleId, await InspectContributorsAsync(roleId, cancellationToken));

    /// <summary>The permission this mid-handler check enforces, matching what the delete endpoints declare.</summary>
    private static readonly Permission DeleteRoles = new(Permissions.IdentityPermissions.Roles, CoreVerbs.Delete);

    // Evaluated through the shared evaluator rather than by claim-value equality. This previously compared
    // against the legacy string "delete:role", which nothing has granted since the vocabulary migration, so
    // every caller except a holder of "*" was refused here after already passing the endpoint's own
    // identity/roles:delete check. Going through the evaluator also lets a wildcard grant such as
    // identity/*:delete reach this path, as it already does at the endpoint.
    private static bool HasPermission(ClaimsPrincipal actor) =>
        PermissionEvaluator.Shared.HasPermission(actor, DeleteRoles);
}
