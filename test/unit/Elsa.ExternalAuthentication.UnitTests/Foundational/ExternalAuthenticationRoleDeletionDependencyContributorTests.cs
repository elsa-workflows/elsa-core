using Elsa.Authorization;
using Elsa.Testing.Shared.Multitenancy;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Policies;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationRoleDeletionDependencyContributorTests
{
    [Fact]
    public async Task InspectIncludesConfigurationAndDatabaseReferencesAcrossLifecycleStates()
    {
        var configurationConnection = Connection(
            "configured",
            new PolicySelection(
                CreateUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "workflow-user", "other-role" } })));
        var databaseConnection = Connection(
            "database",
            new PolicySelection(
                MatchExternalUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { noMatchAction = "create-user", defaultRoleIds = new[] { "workflow-user" } })));
        databaseConnection.ArchivedAt = DateTimeOffset.UtcNow;
        var (contributor, store, _) = await CreateContributorAsync([configurationConnection], databaseConnection);

        var snapshot = await contributor.InspectAsync("workflow-user");

        Assert.False(snapshot.SupportsAtomicRemoval);
        var configuration = Assert.Single(snapshot.Dependencies, x => x.Ownership == RoleDeletionDependencyOwnership.Configuration);
        Assert.Equal("ExternalAuthentication:Connections:0:UnlinkedPolicy:Settings:defaultRoleIds:0", configuration.ConfigurationPath);
        Assert.False(configuration.RemovesLastDefaultRole);
        var database = Assert.Single(snapshot.Dependencies, x => x.Ownership == RoleDeletionDependencyOwnership.Database);
        Assert.Equal("matcher-no-match-create-user", database.PolicyBranch);
        Assert.Equal(1, database.ExpectedRevision);
        Assert.True(database.RemovesLastDefaultRole);
        Assert.NotNull(await store.FindByIdAsync(databaseConnection.Id));
    }

    [Fact]
    public async Task ConfigurationReferenceCannotBeRemediated()
    {
        var configurationConnection = Connection(
            "configured",
            new PolicySelection(
                CreateUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "workflow-user" } })));
        var databaseConnection = Connection(
            "database",
            new PolicySelection(
                CreateUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "workflow-user" } })));
        var (contributor, _, _) = await CreateContributorAsync([configurationConnection], databaseConnection);
        var snapshot = await contributor.InspectAsync("workflow-user");
        var databaseDependencies = snapshot.Dependencies.Where(x => x.Ownership == RoleDeletionDependencyOwnership.Database).ToArray();

        var result = await contributor.ValidateRemovalAsync(new(
            "workflow-user",
            Administrator(),
            snapshot.Version,
            databaseDependencies));

        Assert.IsType<RoleReferenceRemovalValidationResult.Conflict>(result);
        Assert.Equal(
            ["workflow-user"],
            configurationConnection.UnlinkedPolicy!.Settings.GetProperty("defaultRoleIds").EnumerateArray().Select(x => x.GetString()!).ToArray());
    }

    [Fact]
    public async Task RemovesRoleFromEditablePolicyAndAdvancesRegistryVersion()
    {
        var databaseConnection = Connection(
            "database",
            new PolicySelection(
                CreateUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "workflow-user" } })));
        var (contributor, store, versions) = await CreateContributorAsync([], databaseConnection);
        var snapshot = await contributor.InspectAsync("workflow-user");
        var request = new RoleReferenceRemovalRequest(
            "workflow-user",
            Administrator(),
            snapshot.Version,
            snapshot.Dependencies);

        Assert.IsType<RoleReferenceRemovalValidationResult.Valid>(await contributor.ValidateRemovalAsync(request));
        var result = Assert.IsType<RoleReferenceRemovalResult.Success>(await contributor.RemoveEditableReferencesAsync(request));

        Assert.Equal([databaseConnection.Id], result.ChangedOwnerIds);
        var updated = Assert.IsType<IdentityProviderConnection>(await store.FindByIdAsync(databaseConnection.Id));
        Assert.Empty(updated.UnlinkedPolicy!.Settings.GetProperty("defaultRoleIds").EnumerateArray());
        Assert.Equal(2, updated.Revision);
        Assert.True(await versions.GetVersionAsync() > 0);
    }

    [Fact]
    public async Task StaleConnectionRevisionFailsPrevalidationWithoutMutation()
    {
        var databaseConnection = Connection(
            "database",
            new PolicySelection(
                CreateUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "workflow-user", "other-role" } })));
        var (contributor, store, _) = await CreateContributorAsync([], databaseConnection);
        var snapshot = await contributor.InspectAsync("workflow-user");
        var changed = (await store.FindByIdAsync(databaseConnection.Id))!;
        changed.DisplayName = "Changed concurrently";
        Assert.IsType<ConnectionMutationResult.Updated>(await store.UpdateAsync(changed, changed.Revision));

        var result = await contributor.ValidateRemovalAsync(new(
            "workflow-user",
            Administrator(),
            snapshot.Version,
            snapshot.Dependencies));

        Assert.IsType<RoleReferenceRemovalValidationResult.Conflict>(result);
        var current = (await store.FindByIdAsync(databaseConnection.Id))!;
        Assert.Contains("workflow-user", current.UnlinkedPolicy!.Settings.GetProperty("defaultRoleIds").EnumerateArray().Select(x => x.GetString()));
    }

    [Theory]
    [InlineData(ConnectionsUpdate)]
    [InlineData(PoliciesUpdate)]
    [InlineData(DefaultRolesUpdate)]
    public async Task PrevalidationRequiresEveryPolicyRemediationPermission(string omittedPermission)
    {
        var databaseConnection = Connection(
            "database",
            new PolicySelection(
                CreateUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "workflow-user" } })));
        var (contributor, _, _) = await CreateContributorAsync([], databaseConnection);
        var snapshot = await contributor.InspectAsync("workflow-user");

        var result = await contributor.ValidateRemovalAsync(new(
            "workflow-user",
            Administrator(omittedPermission),
            snapshot.Version,
            snapshot.Dependencies));

        Assert.IsType<RoleReferenceRemovalValidationResult.Forbidden>(result);
    }

    private static async Task<(ExternalAuthenticationRoleDeletionDependencyContributor Contributor, InMemoryIdentityProviderConnectionStore Store, InMemoryConnectionRegistryVersionStore Versions)> CreateContributorAsync(
        IReadOnlyCollection<IdentityProviderConnection> configuredConnections,
        params IdentityProviderConnection[] databaseConnections)
    {
        var store = new InMemoryIdentityProviderConnectionStore();
        foreach (var connection in databaseConnections)
            Assert.IsType<ConnectionMutationResult.Created>(await store.CreateAsync(connection));

        var roleStore = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        await roleStore.SaveAsync(new Role { Id = "workflow-user", Name = "Workflow user", Permissions = [] });
        await roleStore.SaveAsync(new Role { Id = "other-role", Name = "Other role", Permissions = [] });
        var versions = new InMemoryConnectionRegistryVersionStore();
        var services = new ServiceCollection().BuildServiceProvider();
        var contributor = new ExternalAuthenticationRoleDeletionDependencyContributor(
            store,
            new MutableOptionsMonitor<ExternalAuthenticationOptions>(new ExternalAuthenticationOptions { ConfigurationConnections = configuredConnections.ToList() }),
            new RoleAuthorizationService(new StoreBasedRoleProvider(roleStore), new PermissionEvaluator()),
            versions,
            new ConnectionRevisionCalculator(),
            new ExternalAuthenticationSecurityNotifier(services),
            new PermissionEvaluator());
        return (contributor, store, versions);
    }

    private static IdentityProviderConnection Connection(string id, PolicySelection policy) => new()
    {
        Id = id,
        TenantId = ConnectionScope.HostTenantId,
        Key = id,
        AdapterType = "oidc",
        AdapterSettingsVersion = 1,
        DisplayName = id,
        IsEnabled = false,
        UnlinkedPolicy = policy,
        MaterialRevision = "test",
        CreatedAt = DateTimeOffset.UnixEpoch,
        UpdatedAt = DateTimeOffset.UnixEpoch
    };

    private const string ConnectionsUpdate = $"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.Update}";
    private const string PoliciesUpdate = $"{ExternalAuthenticationResourcePermissions.Policies}:{CoreVerbs.Update}";
    private const string DefaultRolesUpdate = $"{ExternalAuthenticationResourcePermissions.PolicyDefaultRoles}:{CoreVerbs.Update}";

    private static ClaimsPrincipal Administrator(string? omittedPermission = null)
    {
        var permissions = new[]
        {
            "identity/roles:delete",
            ConnectionsUpdate,
            PoliciesUpdate,
            DefaultRolesUpdate
        };
        return new ClaimsPrincipal(new ClaimsIdentity(
            permissions
                .Where(x => !string.Equals(x, omittedPermission, StringComparison.Ordinal))
                .Select(x => new Claim(PermissionNames.ClaimType, x))));
    }
}
