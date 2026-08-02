using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Policies;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>Guards Elsa Role deletion against JIT-policy default-role references.</summary>
public sealed class ExternalAuthenticationRoleDeletionDependencyContributor(
    IIdentityProviderConnectionStore store,
    IOptionsMonitor<ExternalAuthenticationOptions> options,
    Elsa.Identity.Contracts.IRoleAuthorizationService roleAuthorizationService,
    IConnectionRegistryVersionStore registryVersions,
    ConnectionRevisionCalculator revisionCalculator,
    ExternalAuthenticationSecurityNotifier notifier) : IRoleDeletionDependencyContributor
{
    public const string SourceName = "external-authentication";
    public string Source => SourceName;

    public async ValueTask<RoleDeletionDependencySnapshot> InspectAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var dependencies = new List<RoleDeletionDependency>();
        var configuredConnections = options.CurrentValue.ConfigurationConnections ?? [];
        var configurationIndex = 0;
        foreach (var connection in configuredConnections)
        {
            dependencies.AddRange(GetConfigurationDependencies(connection, configurationIndex, roleId));
            configurationIndex++;
        }

        var databaseConnections = await store.FindAsync(new ConnectionFilter(), cancellationToken);
        foreach (var connection in databaseConnections.Items)
        {
            if (!TryGetRoleReference(connection.UnlinkedPolicy, roleId, out var policyBranch, out _, out var removesLastDefaultRole))
                continue;
            dependencies.Add(new RoleDeletionDependency(
                Source,
                connection.Id,
                connection.Key,
                policyBranch,
                RoleDeletionDependencyOwnership.Database,
                null,
                connection.Revision,
                removesLastDefaultRole));
        }

        var ordered = dependencies
            .OrderBy(x => x.Ownership)
            .ThenBy(x => x.OwnerId, StringComparer.Ordinal)
            .ThenBy(x => x.ConfigurationPath, StringComparer.Ordinal)
            .ToArray();
        return new RoleDeletionDependencySnapshot(Source, CalculateVersion(ordered), false, ordered);
    }

    public async ValueTask<RoleReferenceRemovalValidationResult> ValidateRemovalAsync(RoleReferenceRemovalRequest request, CancellationToken cancellationToken = default)
    {
        if (!HasPermission(request.Actor, ExternalAuthenticationPermissions.ConnectionsUpdate) ||
            !HasPermission(request.Actor, ExternalAuthenticationPermissions.PoliciesManage) ||
            !HasPermission(request.Actor, ExternalAuthenticationPermissions.RolesAssign))
            return new RoleReferenceRemovalValidationResult.Forbidden("missing_policy_permissions");
        if (request.Dependencies.Count == 0 ||
            request.Dependencies.Any(x => x.Ownership != RoleDeletionDependencyOwnership.Database || !string.Equals(x.Source, Source, StringComparison.Ordinal)))
            return new RoleReferenceRemovalValidationResult.Conflict("invalid_dependency_set");

        var current = await InspectAsync(request.RoleId, cancellationToken);
        if (!string.Equals(current.Version, request.ExpectedContributorVersion, StringComparison.Ordinal) ||
            current.Dependencies.Any(x => x.Ownership == RoleDeletionDependencyOwnership.Configuration))
            return new RoleReferenceRemovalValidationResult.Conflict("dependency_changed");

        var expectedOwners = request.Dependencies.Select(x => x.OwnerId).ToHashSet(StringComparer.Ordinal);
        var currentOwners = current.Dependencies
            .Where(x => x.Ownership == RoleDeletionDependencyOwnership.Database)
            .Select(x => x.OwnerId)
            .ToHashSet(StringComparer.Ordinal);
        if (!expectedOwners.SetEquals(currentOwners))
            return new RoleReferenceRemovalValidationResult.Conflict("dependency_changed");

        foreach (var dependency in request.Dependencies)
        {
            var connection = await store.FindByIdAsync(dependency.OwnerId, cancellationToken);
            if (connection is null || connection.Revision != dependency.ExpectedRevision ||
                !TryGetRoleReference(connection.UnlinkedPolicy, request.RoleId, out _, out var roleIds, out _))
                return new RoleReferenceRemovalValidationResult.Conflict("connection_revision_changed");
            var remainingRoleIds = roleIds.Where(x => !string.Equals(x, request.RoleId, StringComparison.Ordinal)).ToArray();
            if (!await roleAuthorizationService.CanAssignRolesAsync(request.Actor, remainingRoleIds, cancellationToken))
                return new RoleReferenceRemovalValidationResult.Forbidden("role_assignment_denied");
        }

        return new RoleReferenceRemovalValidationResult.Valid();
    }

    public async ValueTask<RoleReferenceRemovalResult> RemoveEditableReferencesAsync(RoleReferenceRemovalRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateRemovalAsync(request, cancellationToken);
        if (validation is RoleReferenceRemovalValidationResult.Forbidden forbidden)
            return new RoleReferenceRemovalResult.Failed(forbidden.Code, []);
        if (validation is RoleReferenceRemovalValidationResult.Conflict conflict)
            return new RoleReferenceRemovalResult.Conflict(conflict.Code, []);

        var changedOwnerIds = new List<string>();
        try
        {
            foreach (var dependency in request.Dependencies.OrderBy(x => x.OwnerId, StringComparer.Ordinal))
            {
                var connection = await store.FindByIdAsync(dependency.OwnerId, cancellationToken);
                if (connection is null || connection.Revision != dependency.ExpectedRevision ||
                    !TryGetRoleReference(connection.UnlinkedPolicy, request.RoleId, out _, out _, out _))
                    return new RoleReferenceRemovalResult.Conflict("connection_revision_changed", changedOwnerIds);

                var candidate = IdentityProviderConnectionCloner.Clone(connection);
                candidate.UnlinkedPolicy = candidate.UnlinkedPolicy is { } policy
                    ? policy with { Settings = RemoveRole(policy.Settings, request.RoleId) }
                    : null;
                candidate.UpdatedAt = DateTimeOffset.UtcNow;
                candidate.MaterialRevision = revisionCalculator.CalculateMaterialRevision(candidate);

                var update = await store.UpdateAsync(candidate, connection.Revision, cancellationToken);
                if (update is not ConnectionMutationResult.Updated updated)
                    return new RoleReferenceRemovalResult.Conflict("connection_revision_changed", changedOwnerIds);

                changedOwnerIds.Add(updated.Connection.Id);
                await registryVersions.AdvanceAsync(cancellationToken);
                await notifier.PublishAsync(
                    new IdentityProviderConnectionChanged(
                        ExternalAuthenticationSecurityNotifier.Context(
                            request.Actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? request.Actor.FindFirstValue("sub"),
                            updated.Connection.TenantId,
                            updated.Connection.Id,
                            null,
                            SecurityEventOutcome.Succeeded,
                            "An Elsa Role reference was removed from an external authentication JIT policy."),
                        "default-role-removed",
                        updated.Connection.Revision,
                        updated.Connection.MaterialRevision),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return new RoleReferenceRemovalResult.Failed("storage_error", changedOwnerIds);
        }

        return new RoleReferenceRemovalResult.Success(changedOwnerIds);
    }

    private IEnumerable<RoleDeletionDependency> GetConfigurationDependencies(IdentityProviderConnection connection, int connectionIndex, string roleId)
    {
        if (!TryGetRoleReference(connection.UnlinkedPolicy, roleId, out var policyBranch, out var roleIds, out var removesLastDefaultRole))
            yield break;

        var roleIndex = 0;
        foreach (var configuredRoleId in ReadRoleIdsWithDuplicates(connection.UnlinkedPolicy!.Settings))
        {
            if (string.Equals(configuredRoleId, roleId, StringComparison.Ordinal))
            {
                yield return new RoleDeletionDependency(
                    Source,
                    string.IsNullOrWhiteSpace(connection.Id) ? $"configuration:{connectionIndex}" : connection.Id,
                    connection.Key,
                    policyBranch,
                    RoleDeletionDependencyOwnership.Configuration,
                    $"ExternalAuthentication:Connections:{connectionIndex}:UnlinkedPolicy:Settings:defaultRoleIds:{roleIndex}",
                    null,
                    removesLastDefaultRole);
            }

            roleIndex++;
        }
    }

    private static bool TryGetRoleReference(PolicySelection? policy, string roleId, out string policyBranch, out IReadOnlyCollection<string> roleIds, out bool removesLastDefaultRole)
    {
        policyBranch = string.Empty;
        roleIds = [];
        removesLastDefaultRole = false;
        if (policy is null)
            return false;

        if (string.Equals(policy.Type, CreateUserUnlinkedIdentityPolicy.PolicyType, StringComparison.Ordinal))
            policyBranch = "create-user";
        else if (string.Equals(policy.Type, MatchExternalUserUnlinkedIdentityPolicy.PolicyType, StringComparison.Ordinal) &&
                 string.Equals(ReadString(policy.Settings, "noMatchAction"), "create-user", StringComparison.OrdinalIgnoreCase))
            policyBranch = "matcher-no-match-create-user";
        else
            return false;

        roleIds = CreateUserUnlinkedIdentityPolicy.ReadRoleIds(policy.Settings);
        if (!roleIds.Contains(roleId, StringComparer.Ordinal))
            return false;
        removesLastDefaultRole = roleIds.Count == 1;
        return true;
    }

    private static JsonElement RemoveRole(JsonElement settings, string roleId)
    {
        var root = settings.ValueKind == JsonValueKind.Object
            ? JsonNode.Parse(settings.GetRawText()) as JsonObject
            : new JsonObject();
        root ??= new JsonObject();
        var remainingRoleIds = ReadRoleIdsWithDuplicates(settings)
            .Where(x => !string.Equals(x, roleId, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var roleNodes = new JsonNode?[remainingRoleIds.Length];
        for (var index = 0; index < remainingRoleIds.Length; index++)
            roleNodes[index] = JsonValue.Create(remainingRoleIds[index]);
        root["defaultRoleIds"] = new JsonArray(roleNodes);
        return JsonSerializer.SerializeToElement(root);
    }

    private static IReadOnlyCollection<string> ReadRoleIdsWithDuplicates(JsonElement settings) =>
        settings.ValueKind == JsonValueKind.Object &&
        settings.TryGetProperty("defaultRoleIds", out var values) &&
        values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToArray()
            : [];

    private static string? ReadString(JsonElement settings, string propertyName) =>
        settings.ValueKind == JsonValueKind.Object &&
        settings.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool HasPermission(ClaimsPrincipal actor, string permission) =>
        actor.FindAll(PermissionNames.ClaimType).Any(x => x.Value == PermissionNames.All || string.Equals(x.Value, permission, StringComparison.Ordinal));

    private static string CalculateVersion(IEnumerable<RoleDeletionDependency> dependencies)
    {
        var payload = string.Join(
            "\n",
            dependencies.Select(x => $"{x.OwnerId}|{x.OwnerKey}|{x.PolicyBranch}|{x.Ownership}|{x.ConfigurationPath}|{x.ExpectedRevision}|{x.RemovesLastDefaultRole}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
