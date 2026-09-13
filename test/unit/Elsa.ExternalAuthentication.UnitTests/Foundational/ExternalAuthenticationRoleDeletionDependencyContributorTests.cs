using Elsa.Authorization;
using Elsa.Testing.Shared.Multitenancy;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Policies;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationRoleDeletionDependencyContributorTests
{
    [Test]
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

        await Assert.That(snapshot.SupportsAtomicRemoval).IsFalse();
        var configuration = await Assert.That(snapshot.Dependencies).HasSingleItem(x => x.Ownership == RoleDeletionDependencyOwnership.Configuration);
        await Assert.That(configuration.ConfigurationPath).IsEqualTo("ExternalAuthentication:Connections:0:UnlinkedPolicy:Settings:defaultRoleIds:0");
        await Assert.That(configuration.RemovesLastDefaultRole).IsFalse();
        var database = await Assert.That(snapshot.Dependencies).HasSingleItem(x => x.Ownership == RoleDeletionDependencyOwnership.Database);
        await Assert.That(database.PolicyBranch).IsEqualTo("matcher-no-match-create-user");
        await Assert.That(database.ExpectedRevision).IsEqualTo(1);
        await Assert.That(database.RemovesLastDefaultRole).IsTrue();
        await Assert.That(await store.FindByIdAsync(databaseConnection.Id)).IsNotNull();
    }

    [Test]
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

        await Assert.That(result).IsOfType(typeof(RoleReferenceRemovalValidationResult.Conflict));
        await Assert.That(configurationConnection.UnlinkedPolicy!.Settings.GetProperty("defaultRoleIds").EnumerateArray().Select(x => x.GetString()!).ToArray()).IsEquivalentTo(["workflow-user"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
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

        await Assert.That(await contributor.ValidateRemovalAsync(request)).IsOfType(typeof(RoleReferenceRemovalValidationResult.Valid));
        var removalResult = await contributor.RemoveEditableReferencesAsync(request);
        await Assert.That(removalResult).IsOfType(typeof(RoleReferenceRemovalResult.Success));
        var result = (RoleReferenceRemovalResult.Success)removalResult;

        await Assert.That(result.ChangedOwnerIds).IsEquivalentTo([databaseConnection.Id], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        var updated = await Assert.That(await store.FindByIdAsync(databaseConnection.Id)).IsNotNull();
        await Assert.That(updated).IsOfType(typeof(IdentityProviderConnection));
        await Assert.That(updated.UnlinkedPolicy!.Settings.GetProperty("defaultRoleIds").EnumerateArray()).IsEmpty();
        await Assert.That(updated.Revision).IsEqualTo(2);
        await Assert.That(await versions.GetVersionAsync() > 0).IsTrue();
    }

    [Test]
    public async Task ReplacesFinalDefaultRoleWhenSelectedForRemediation()
    {
        var databaseConnection = Connection(
            "database",
            new PolicySelection(
                CreateUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "workflow-user" } })));
        var (contributor, store, _) = await CreateContributorAsync([], [databaseConnection], [new Role { Id = "replacement-role", Name = "Replacement role", Permissions = [] }]);
        var snapshot = await contributor.InspectAsync("workflow-user");
        var request = new RoleReferenceRemovalRequest(
            "workflow-user",
            Administrator(),
            snapshot.Version,
            snapshot.Dependencies)
        {
            SelectedReferences = [new RoleDeletionReferenceSelection(ExternalAuthenticationRoleDeletionDependencyContributor.SourceName, databaseConnection.Id)],
            ReplacementRoleId = "replacement-role"
        };

        await Assert.That(await contributor.ValidateRemovalAsync(request)).IsOfType(typeof(RoleReferenceRemovalValidationResult.Valid));
        var removalResult = await contributor.RemoveEditableReferencesAsync(request);
        await Assert.That(removalResult).IsOfType(typeof(RoleReferenceRemovalResult.Success));
        var result = (RoleReferenceRemovalResult.Success)removalResult;

        await Assert.That(result.ChangedOwnerIds).IsEquivalentTo([databaseConnection.Id], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        var updated = await Assert.That(await store.FindByIdAsync(databaseConnection.Id)).IsNotNull();
        await Assert.That(updated).IsOfType(typeof(IdentityProviderConnection));
        await Assert.That(updated.UnlinkedPolicy!.Settings.GetProperty("defaultRoleIds").EnumerateArray().Select(x => x.GetString()!).ToArray()).IsEquivalentTo(["replacement-role"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task UsesTheActiveRoleStoreWhenPersistenceReplacesTheDefaultStore()
    {
        var databaseConnection = Connection(
            "database",
            new PolicySelection(
                CreateUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "workflow-user" } })));
        var connectionStore = new InMemoryIdentityProviderConnectionStore();
        await Assert.That(await connectionStore.CreateAsync(databaseConnection)).IsOfType(typeof(ConnectionMutationResult.Created));
        var replacedStore = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        var activeStore = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        await activeStore.SaveAsync(new Role { Id = "workflow-user", Name = "Workflow user", Permissions = [] });
        await activeStore.SaveAsync(new Role { Id = "replacement-role", Name = "Replacement role", Permissions = [] });
        var roleAuthorizationService = new RoleAuthorizationService(new StoreBasedRoleProvider(activeStore), new PermissionEvaluator());
        var services = new ServiceCollection().BuildServiceProvider();
        var contributor = new ExternalAuthenticationRoleDeletionDependencyContributor(
            connectionStore,
            new MutableOptionsMonitor<ExternalAuthenticationOptions>(new ExternalAuthenticationOptions()),
            [roleAuthorizationService],
            [replacedStore, activeStore],
            new InMemoryConnectionRegistryVersionStore(),
            new ConnectionRevisionCalculator(),
            new ExternalAuthenticationSecurityNotifier(services),
            new PermissionEvaluator(),
            TestTenantAccessor.Default);
        var snapshot = await contributor.InspectAsync("workflow-user");
        var request = new RoleReferenceRemovalRequest(
            "workflow-user",
            Administrator(),
            snapshot.Version,
            snapshot.Dependencies)
        {
            SelectedReferences = [new RoleDeletionReferenceSelection(ExternalAuthenticationRoleDeletionDependencyContributor.SourceName, databaseConnection.Id)],
            ReplacementRoleId = "replacement-role"
        };

        await Assert.That(await contributor.ValidateRemovalAsync(request)).IsOfType(typeof(RoleReferenceRemovalValidationResult.Valid));
        await Assert.That(await contributor.RemoveEditableReferencesAsync(request)).IsOfType(typeof(RoleReferenceRemovalResult.Success));
    }

    [Test]
    public async Task RejectsMissingReplacementForSelectedFinalDefaultRole()
    {
        var databaseConnection = Connection(
            "database",
            new PolicySelection(
                CreateUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "workflow-user" } })));
        var (contributor, store, _) = await CreateContributorAsync([], databaseConnection);
        var snapshot = await contributor.InspectAsync("workflow-user");
        var request = new RoleReferenceRemovalRequest(
            "workflow-user",
            Administrator(),
            snapshot.Version,
            snapshot.Dependencies)
        {
            SelectedReferences = [new RoleDeletionReferenceSelection(ExternalAuthenticationRoleDeletionDependencyContributor.SourceName, databaseConnection.Id)]
        };

        var result = await contributor.ValidateRemovalAsync(request);

        await Assert.That(result).IsOfType(typeof(RoleReferenceRemovalValidationResult.Forbidden));
        var forbidden = (RoleReferenceRemovalValidationResult.Forbidden)result;
        await Assert.That(forbidden.Code).IsEqualTo("replacement_role_unavailable_or_unauthorized");
        var current = await Assert.That(await store.FindByIdAsync(databaseConnection.Id)).IsNotNull();
        await Assert.That(current).IsOfType(typeof(IdentityProviderConnection));
        await Assert.That(current.UnlinkedPolicy!.Settings.GetProperty("defaultRoleIds").EnumerateArray().Select(x => x.GetString()!).ToArray()).IsEquivalentTo(["workflow-user"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task ReplacementRemovedAfterCoordinatorValidationFailsClosedBeforePolicyUpdate()
    {
        var databaseConnection = Connection(
            "database",
            new PolicySelection(
                CreateUserUnlinkedIdentityPolicy.PolicyType,
                1,
                JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "workflow-user" } })));
        var connectionStore = new InMemoryIdentityProviderConnectionStore();
        await Assert.That(await connectionStore.CreateAsync(databaseConnection)).IsOfType(typeof(ConnectionMutationResult.Created));

        var backingRoleStore = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        await backingRoleStore.SaveAsync(new Role { Id = "workflow-user", Name = "Workflow user", Permissions = [] });
        await backingRoleStore.SaveAsync(new Role { Id = "replacement-role", Name = "Replacement role", Permissions = [] });
        var roleStore = new RoleStoreThatRemovesReplacementAfterContributorValidation(backingRoleStore, "replacement-role");
        var roleAuthorizationService = new RoleAuthorizationService(new StoreBasedRoleProvider(roleStore), new PermissionEvaluator());
        var versions = new InMemoryConnectionRegistryVersionStore();
        var services = new ServiceCollection().BuildServiceProvider();
        var contributor = new ExternalAuthenticationRoleDeletionDependencyContributor(
            connectionStore,
            new MutableOptionsMonitor<ExternalAuthenticationOptions>(new ExternalAuthenticationOptions()),
            [roleAuthorizationService],
            [roleStore],
            versions,
            new ConnectionRevisionCalculator(),
            new ExternalAuthenticationSecurityNotifier(services),
            new PermissionEvaluator(),
            TestTenantAccessor.Default);
        var securityNotifier = new RoleSecurityNotifier(Substitute.For<INotificationSender>(), TestTenantAccessor.Default, new SystemClock());
        var coordinator = new RoleDeletionCoordinator(roleStore, roleAuthorizationService, [contributor], securityNotifier);
        var inspection = await coordinator.InspectAsync("workflow-user", Administrator());
        await Assert.That(inspection).IsOfType(typeof(RoleDeletionInspectionResult.Success));
        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;

        var result = await coordinator.RemediateAndDeleteAsync(new RoleDeletionRemediationCommand(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            true,
            true,
            true)
        {
            SelectedReferences = [new RoleDeletionReferenceSelection(ExternalAuthenticationRoleDeletionDependencyContributor.SourceName, databaseConnection.Id)],
            ReplacementRoleId = "replacement-role"
        });

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.Incomplete));
        var incomplete = (RoleDeletionOperationResult.Incomplete)result;
        await Assert.That(incomplete.Code).IsEqualTo("replacement_role_unavailable_or_unauthorized");
        await Assert.That(roleStore.ReplacementRemoved).IsTrue();
        await Assert.That(await roleStore.FindAsync(new() { Id = "workflow-user" })).IsNotNull();
        var current = await Assert.That(await connectionStore.FindByIdAsync(databaseConnection.Id)).IsNotNull();
        await Assert.That(current).IsOfType(typeof(IdentityProviderConnection));
        await Assert.That(current.UnlinkedPolicy!.Settings.GetProperty("defaultRoleIds").EnumerateArray().Select(x => x.GetString()!).ToArray()).IsEquivalentTo(["workflow-user"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
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
        await Assert.That(await store.UpdateAsync(changed, changed.Revision)).IsOfType(typeof(ConnectionMutationResult.Updated));

        var result = await contributor.ValidateRemovalAsync(new(
            "workflow-user",
            Administrator(),
            snapshot.Version,
            snapshot.Dependencies));

        await Assert.That(result).IsOfType(typeof(RoleReferenceRemovalValidationResult.Conflict));
        var current = (await store.FindByIdAsync(databaseConnection.Id))!;
        await Assert.That(current.UnlinkedPolicy!.Settings.GetProperty("defaultRoleIds").EnumerateArray().Select(x => x.GetString())).Contains("workflow-user");
    }

    [Test]
    [Arguments(ConnectionsUpdate)]
    [Arguments(PoliciesUpdate)]
    [Arguments(DefaultRolesUpdate)]
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

        await Assert.That(result).IsOfType(typeof(RoleReferenceRemovalValidationResult.Forbidden));
    }

    [Test]
    public async Task ImpactExcludesConnectionsOwnedByAnotherTenant()
    {
        var ownConnection = Connection("own-connection", CreateUserPolicy("workflow-user"), TenantA);
        var otherTenantConnection = Connection("other-tenant-connection", CreateUserPolicy("workflow-user"), TenantB);
        var otherTenantConfiguration = Connection("other-tenant-configured", CreateUserPolicy("workflow-user"), TenantB);
        var (contributor, _, _) = await CreateContributorAsync(
            [otherTenantConfiguration],
            [ownConnection, otherTenantConnection],
            tenantAccessor: new TestTenantAccessor(TenantA));

        var snapshot = await contributor.InspectAsync("workflow-user");

        // Neither tenant B's stored connection nor its configuration entry -- which would block deletion
        // outright -- is reported, while the tenant's own reference still is, so the filter is not simply
        // reporting nothing.
        var dependency = await Assert.That(snapshot.Dependencies).HasSingleItem();
        await Assert.That(dependency.OwnerId).IsEqualTo(ownConnection.Id);
        await Assert.That(dependency.Ownership).IsEqualTo(RoleDeletionDependencyOwnership.Database);
    }

    [Test]
    public async Task ImpactIncludesHostScopedConnectionsForEveryTenant()
    {
        var hostConnection = Connection("host-connection", CreateUserPolicy("workflow-user"));
        var hostConfiguration = Connection("host-configured", CreateUserPolicy("workflow-user"), ConnectionScope.DefaultTenantId);
        var (contributor, _, _) = await CreateContributorAsync(
            [hostConfiguration],
            [hostConnection],
            tenantAccessor: new TestTenantAccessor(TenantA));

        var snapshot = await contributor.InspectAsync("workflow-user");

        // A host connection is served to every signing-in tenant and its default roles are resolved in that
        // tenant, so it references this tenant's role. A blank configuration tenant is materialized at host scope.
        await Assert.That(snapshot.Dependencies.Select(x => x.OwnerId).Order(StringComparer.Ordinal).ToArray()).IsEquivalentTo([hostConfiguration.Id, hostConnection.Id], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task RemediationCannotReachAnotherTenantsConnection()
    {
        var ownConnection = Connection("own-connection", CreateUserPolicy("workflow-user", "other-role"), TenantA);
        var otherTenantConnection = Connection("other-tenant-connection", CreateUserPolicy("workflow-user", "other-role"), TenantB);
        var (contributor, store, _) = await CreateContributorAsync(
            [],
            [ownConnection, otherTenantConnection],
            tenantAccessor: new TestTenantAccessor(TenantA));
        var snapshot = await contributor.InspectAsync("workflow-user");
        var request = new RoleReferenceRemovalRequest(
            "workflow-user",
            Administrator(),
            snapshot.Version,
            [
                ..snapshot.Dependencies,
                // The owner ID of another tenant's connection, as a caller could supply it.
                new RoleDeletionDependency(
                    ExternalAuthenticationRoleDeletionDependencyContributor.SourceName,
                    otherTenantConnection.Id,
                    otherTenantConnection.Key,
                    "create-user",
                    RoleDeletionDependencyOwnership.Database,
                    null,
                    1,
                    false)
            ]);

        await Assert.That(await contributor.ValidateRemovalAsync(request)).IsOfType(typeof(RoleReferenceRemovalValidationResult.Conflict));
        var removalResult = await contributor.RemoveEditableReferencesAsync(request);
        await Assert.That(removalResult).IsOfType(typeof(RoleReferenceRemovalResult.Conflict));
        var result = (RoleReferenceRemovalResult.Conflict)removalResult;

        // Nothing may half-run: neither the foreign connection nor the tenant's own connection is touched.
        await Assert.That(result.ChangedOwnerIds).IsEmpty();
        await AssertDefaultRoleIds(await store.FindByIdAsync(otherTenantConnection.Id), "workflow-user", "other-role");
        await AssertDefaultRoleIds(await store.FindByIdAsync(ownConnection.Id), "workflow-user", "other-role");
    }

    [Test]
    public async Task RemediationStopsWhenTheConnectionLeavesTheRoleTenantAfterValidation()
    {
        var ownConnection = Connection("own-connection", CreateUserPolicy("workflow-user", "other-role"), TenantA);
        var (contributor, store, _) = await CreateContributorAsync(
            [],
            [ownConnection],
            tenantAccessor: new TestTenantAccessor(TenantA),
            decorateStore: inner => new ConnectionStoreThatMovesConnectionToAnotherTenant(inner, ownConnection.Id, TenantB, lookupsBeforeMove: 1));
        var snapshot = await contributor.InspectAsync("workflow-user");
        var request = new RoleReferenceRemovalRequest("workflow-user", Administrator(), snapshot.Version, snapshot.Dependencies);

        var removalResult = await contributor.RemoveEditableReferencesAsync(request);
        await Assert.That(removalResult).IsOfType(typeof(RoleReferenceRemovalResult.Conflict));
        var result = (RoleReferenceRemovalResult.Conflict)removalResult;

        await Assert.That(result.Code).IsEqualTo("connection_revision_changed");
        await Assert.That(result.ChangedOwnerIds).IsEmpty();
        await AssertDefaultRoleIds(await store.FindByIdAsync(ownConnection.Id), "workflow-user", "other-role");
    }

    [Test]
    public async Task ImpactForAnAgnosticRoleIncludesAConnectionOwnedByAnotherTenant()
    {
        var ownConnection = Connection("own-connection", CreateUserPolicy("agnostic-role"), TenantA);
        var otherTenantConnection = Connection("other-tenant-connection", CreateUserPolicy("agnostic-role"), TenantB);
        var (contributor, _, _) = await CreateContributorAsync(
            [],
            [ownConnection, otherTenantConnection],
            additionalRoles: [new Role { Id = "agnostic-role", Name = "Agnostic role", TenantId = Tenant.AgnosticTenantId, Permissions = [] }],
            tenantAccessor: new TestTenantAccessor(TenantA));

        var snapshot = await contributor.InspectAsync("agnostic-role");

        // The role is visible from every tenant, so its tenant context is every tenant: tenant B's reference is
        // reported alongside tenant A's, unlike a tenant-scoped role (see ImpactExcludesConnectionsOwnedByAnotherTenant).
        await Assert.That(snapshot.Dependencies.Select(x => x.OwnerId).Order(StringComparer.Ordinal).ToArray()).IsEquivalentTo([otherTenantConnection.Id, ownConnection.Id], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task ImpactIsScopedByTheResolvedRolesTenantRatherThanTheAmbientTenant()
    {
        // The ambient tenant is the default tenant (no tenant pushed), which is what an EF host runs as with
        // multitenancy disabled: the EF role store installs no tenant query filter there and can resolve a
        // tenant-owned role by ID regardless of the ambient tenant, unlike MemoryRoleStore, which always
        // filters by the ambient tenant itself. RoleStoreWithoutAmbientTenantFilter stands in for that EF
        // behavior. The role being deleted belongs to tenant A, so impact must be scoped by that resolved
        // tenant, not by the unrelated ambient one.
        var ownConnection = Connection("own-connection", CreateUserPolicy("tenant-a-role"), TenantA);
        var otherTenantConnection = Connection("other-tenant-connection", CreateUserPolicy("tenant-a-role"), TenantB);
        var connectionStore = new InMemoryIdentityProviderConnectionStore();
        await Assert.That(await connectionStore.CreateAsync(ownConnection)).IsOfType(typeof(ConnectionMutationResult.Created));
        await Assert.That(await connectionStore.CreateAsync(otherTenantConnection)).IsOfType(typeof(ConnectionMutationResult.Created));
        var roleStore = new RoleStoreWithoutAmbientTenantFilter(
            [new Role { Id = "tenant-a-role", Name = "Tenant A role", TenantId = TenantA, Permissions = [] }]);
        var roleAuthorizationService = new RoleAuthorizationService(new StoreBasedRoleProvider(roleStore), new PermissionEvaluator());
        var services = new ServiceCollection().BuildServiceProvider();
        var contributor = new ExternalAuthenticationRoleDeletionDependencyContributor(
            connectionStore,
            new MutableOptionsMonitor<ExternalAuthenticationOptions>(new ExternalAuthenticationOptions()),
            [roleAuthorizationService],
            [roleStore],
            new InMemoryConnectionRegistryVersionStore(),
            new ConnectionRevisionCalculator(),
            new ExternalAuthenticationSecurityNotifier(services),
            new PermissionEvaluator(),
            TestTenantAccessor.Default);

        var snapshot = await contributor.InspectAsync("tenant-a-role");

        // Scoped by the role's own tenant (A): tenant A's connection is reported, tenant B's is not. Scoping by
        // the ambient default tenant instead -- what this test is guarding against -- would report neither.
        var dependency = await Assert.That(snapshot.Dependencies).HasSingleItem();
        await Assert.That(dependency.OwnerId).IsEqualTo(ownConnection.Id);
        await Assert.That(dependency.Ownership).IsEqualTo(RoleDeletionDependencyOwnership.Database);
    }

    [Test]
    public async Task RoleIdThatResolvesToMoreThanOneRoleAcrossTenantScopesFailsClosed()
    {
        var ownConnection = Connection("own-connection", CreateUserPolicy("workflow-user"), TenantA);
        var otherTenantConnection = Connection("other-tenant-connection", CreateUserPolicy("workflow-user"), TenantB);
        var (contributor, _, _) = await CreateContributorAsync(
            [],
            [ownConnection, otherTenantConnection],
            additionalRoles: [new Role { Id = "workflow-user", Name = "Agnostic workflow user", TenantId = Tenant.AgnosticTenantId, Permissions = [] }],
            tenantAccessor: new TestTenantAccessor(TenantA));

        // Tenant A's own "workflow-user" role and an agnostic role sharing that same ID both exist in the
        // in-memory role store, so the deletion target is ambiguous: the contributor cannot determine whether
        // to scope its inspection and remediation to tenant A alone or to every tenant, and must fail closed
        // rather than guess in either direction.
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => contributor.InspectAsync("workflow-user").AsTask());

        var request = new RoleReferenceRemovalRequest(
            "workflow-user",
            Administrator(),
            "irrelevant-version",
            [
                new RoleDeletionDependency(
                    ExternalAuthenticationRoleDeletionDependencyContributor.SourceName,
                    ownConnection.Id,
                    ownConnection.Key,
                    "create-user",
                    RoleDeletionDependencyOwnership.Database,
                    null,
                    1,
                    false)
            ]);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => contributor.ValidateRemovalAsync(request).AsTask());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => contributor.RemoveEditableReferencesAsync(request).AsTask());
    }

    [Test]
    public async Task RemediationOfAnAgnosticRoleCanRemoveTheReferenceFromAnotherTenantsConnection()
    {
        var ownConnection = Connection("own-connection", CreateUserPolicy("agnostic-role"), TenantA);
        var otherTenantConnection = Connection("other-tenant-connection", CreateUserPolicy("agnostic-role"), TenantB);
        var (contributor, store, _) = await CreateContributorAsync(
            [],
            [ownConnection, otherTenantConnection],
            additionalRoles: [new Role { Id = "agnostic-role", Name = "Agnostic role", TenantId = Tenant.AgnosticTenantId, Permissions = [] }],
            tenantAccessor: new TestTenantAccessor(TenantA));
        var snapshot = await contributor.InspectAsync("agnostic-role");
        var request = new RoleReferenceRemovalRequest("agnostic-role", Administrator(), snapshot.Version, snapshot.Dependencies);

        await Assert.That(await contributor.ValidateRemovalAsync(request)).IsOfType(typeof(RoleReferenceRemovalValidationResult.Valid));
        var removalResult = await contributor.RemoveEditableReferencesAsync(request);
        await Assert.That(removalResult).IsOfType(typeof(RoleReferenceRemovalResult.Success));
        var result = (RoleReferenceRemovalResult.Success)removalResult;

        await Assert.That(result.ChangedOwnerIds.Order(StringComparer.Ordinal).ToArray()).IsEquivalentTo([otherTenantConnection.Id, ownConnection.Id], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await AssertDefaultRoleIds(await store.FindByIdAsync(ownConnection.Id));
        await AssertDefaultRoleIds(await store.FindByIdAsync(otherTenantConnection.Id));
    }

    [Test]
    public async Task RemediationOfAnAgnosticRoleRejectsATenantScopedReplacementRole()
    {
        var ownConnection = Connection("own-connection", CreateUserPolicy("agnostic-role"), TenantA);
        var otherTenantConnection = Connection("other-tenant-connection", CreateUserPolicy("agnostic-role"), TenantB);
        var (contributor, store, _) = await CreateContributorAsync(
            [],
            [ownConnection, otherTenantConnection],
            additionalRoles:
            [
                new Role { Id = "agnostic-role", Name = "Agnostic role", TenantId = Tenant.AgnosticTenantId, Permissions = [] },
                new Role { Id = "tenant-a-replacement", Name = "Tenant A replacement", TenantId = TenantA, Permissions = [] }
            ],
            tenantAccessor: new TestTenantAccessor(TenantA));
        var snapshot = await contributor.InspectAsync("agnostic-role");
        var request = new RoleReferenceRemovalRequest("agnostic-role", Administrator(), snapshot.Version, snapshot.Dependencies)
        {
            SelectedReferences = snapshot.Dependencies
                .Select(x => new RoleDeletionReferenceSelection(ExternalAuthenticationRoleDeletionDependencyContributor.SourceName, x.OwnerId))
                .ToArray(),
            ReplacementRoleId = "tenant-a-replacement"
        };

        // Remediation is initiated in tenant A and would resolve the replacement role through tenant A's role
        // authorization service alone, even though tenant B's connection is also in scope for this agnostic
        // role. Admitting a tenant-A-only replacement would write a role into tenant B's policy that does not
        // exist there, so it must be rejected rather than authorized in one tenant and applied to every tenant.
        var validation = await contributor.ValidateRemovalAsync(request);
        await Assert.That(validation).IsOfType(typeof(RoleReferenceRemovalValidationResult.Forbidden));
        var forbidden = (RoleReferenceRemovalValidationResult.Forbidden)validation;
        await Assert.That(forbidden.Code).IsEqualTo("replacement_role_unavailable_or_unauthorized");

        var result = await contributor.RemoveEditableReferencesAsync(request);
        await Assert.That(result).IsOfType(typeof(RoleReferenceRemovalResult.Failed));
        var failed = (RoleReferenceRemovalResult.Failed)result;
        await Assert.That(failed.Code).IsEqualTo("replacement_role_unavailable_or_unauthorized");
        await Assert.That(failed.ChangedOwnerIds).IsEmpty();
        await AssertDefaultRoleIds(await store.FindByIdAsync(ownConnection.Id), "agnostic-role");
        await AssertDefaultRoleIds(await store.FindByIdAsync(otherTenantConnection.Id), "agnostic-role");
    }

    [Test]
    public async Task RemediationOfAnAgnosticRoleAcceptsAnAgnosticReplacementRole()
    {
        var ownConnection = Connection("own-connection", CreateUserPolicy("agnostic-role"), TenantA);
        var otherTenantConnection = Connection("other-tenant-connection", CreateUserPolicy("agnostic-role"), TenantB);
        var (contributor, store, _) = await CreateContributorAsync(
            [],
            [ownConnection, otherTenantConnection],
            additionalRoles:
            [
                new Role { Id = "agnostic-role", Name = "Agnostic role", TenantId = Tenant.AgnosticTenantId, Permissions = [] },
                new Role { Id = "agnostic-replacement", Name = "Agnostic replacement", TenantId = Tenant.AgnosticTenantId, Permissions = [] }
            ],
            tenantAccessor: new TestTenantAccessor(TenantA));
        var snapshot = await contributor.InspectAsync("agnostic-role");
        var request = new RoleReferenceRemovalRequest("agnostic-role", Administrator(), snapshot.Version, snapshot.Dependencies)
        {
            SelectedReferences = snapshot.Dependencies
                .Select(x => new RoleDeletionReferenceSelection(ExternalAuthenticationRoleDeletionDependencyContributor.SourceName, x.OwnerId))
                .ToArray(),
            ReplacementRoleId = "agnostic-replacement"
        };

        // An agnostic replacement exists identically in every tenant, so it is safe to write into tenant B's
        // policy even though remediation was authorized through tenant A's role services.
        await Assert.That(await contributor.ValidateRemovalAsync(request)).IsOfType(typeof(RoleReferenceRemovalValidationResult.Valid));
        var removalResult = await contributor.RemoveEditableReferencesAsync(request);
        await Assert.That(removalResult).IsOfType(typeof(RoleReferenceRemovalResult.Success));
        var result = (RoleReferenceRemovalResult.Success)removalResult;

        await Assert.That(result.ChangedOwnerIds.Order(StringComparer.Ordinal).ToArray()).IsEquivalentTo([otherTenantConnection.Id, ownConnection.Id], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await AssertDefaultRoleIds(await store.FindByIdAsync(ownConnection.Id), "agnostic-replacement");
        await AssertDefaultRoleIds(await store.FindByIdAsync(otherTenantConnection.Id), "agnostic-replacement");
    }

    [Test]
    public async Task RemediationRemovesTheRoleFromAHostScopedConnectionForATenant()
    {
        var hostConnection = Connection("host-connection", CreateUserPolicy("workflow-user", "other-role"));
        var (contributor, store, _) = await CreateContributorAsync(
            [],
            [hostConnection],
            tenantAccessor: new TestTenantAccessor(TenantA));
        var snapshot = await contributor.InspectAsync("workflow-user");
        var request = new RoleReferenceRemovalRequest("workflow-user", Administrator(), snapshot.Version, snapshot.Dependencies);

        await Assert.That(await contributor.ValidateRemovalAsync(request)).IsOfType(typeof(RoleReferenceRemovalValidationResult.Valid));
        var removalResult = await contributor.RemoveEditableReferencesAsync(request);
        await Assert.That(removalResult).IsOfType(typeof(RoleReferenceRemovalResult.Success));
        var result = (RoleReferenceRemovalResult.Success)removalResult;

        await Assert.That(result.ChangedOwnerIds).IsEquivalentTo([hostConnection.Id], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await AssertDefaultRoleIds(await store.FindByIdAsync(hostConnection.Id), "other-role");
    }

    [Test]
    public async Task ReplacementRoleIdThatResolvesToBothATenantRoleAndAnAgnosticRoleIsRejectedRatherThanThrowing()
    {
        var ownConnection = Connection("own-connection", CreateUserPolicy("agnostic-role"), TenantA);
        var otherTenantConnection = Connection("other-tenant-connection", CreateUserPolicy("agnostic-role"), TenantB);
        var (contributor, store, _) = await CreateContributorAsync(
            [],
            [ownConnection, otherTenantConnection],
            additionalRoles:
            [
                new Role { Id = "agnostic-role", Name = "Agnostic role", TenantId = Tenant.AgnosticTenantId, Permissions = [] },
                new Role { Id = "ambiguous-replacement", Name = "Ambiguous replacement (tenant A)", TenantId = TenantA, Permissions = [] },
                new Role { Id = "ambiguous-replacement", Name = "Ambiguous replacement (agnostic)", TenantId = Tenant.AgnosticTenantId, Permissions = [] }
            ],
            tenantAccessor: new TestTenantAccessor(TenantA));
        var snapshot = await contributor.InspectAsync("agnostic-role");
        var request = new RoleReferenceRemovalRequest("agnostic-role", Administrator(), snapshot.Version, snapshot.Dependencies)
        {
            SelectedReferences = snapshot.Dependencies
                .Select(x => new RoleDeletionReferenceSelection(ExternalAuthenticationRoleDeletionDependencyContributor.SourceName, x.OwnerId))
                .ToArray(),
            ReplacementRoleId = "ambiguous-replacement"
        };

        // The replacement ID resolves to two roles in the in-memory store (a tenant-A role and an agnostic role
        // sharing the same ID), which is exactly the collision ResolveRoleTenantIdAsync fails closed on for a
        // deletion target. A replacement candidate is not the coordinator's own deletion target, so this must be
        // reported as an ordinary validation failure rather than escape as an exception.
        var validation = await contributor.ValidateRemovalAsync(request);
        await Assert.That(validation).IsOfType(typeof(RoleReferenceRemovalValidationResult.Forbidden));
        var forbidden = (RoleReferenceRemovalValidationResult.Forbidden)validation;
        await Assert.That(forbidden.Code).IsEqualTo("replacement_role_unavailable_or_unauthorized");

        var result = await contributor.RemoveEditableReferencesAsync(request);
        await Assert.That(result).IsOfType(typeof(RoleReferenceRemovalResult.Failed));
        var failed = (RoleReferenceRemovalResult.Failed)result;
        await Assert.That(failed.Code).IsEqualTo("replacement_role_unavailable_or_unauthorized");
        await Assert.That(failed.ChangedOwnerIds).IsEmpty();
        await AssertDefaultRoleIds(await store.FindByIdAsync(ownConnection.Id), "agnostic-role");
        await AssertDefaultRoleIds(await store.FindByIdAsync(otherTenantConnection.Id), "agnostic-role");
    }

    private static Task<(ExternalAuthenticationRoleDeletionDependencyContributor Contributor, InMemoryIdentityProviderConnectionStore Store, InMemoryConnectionRegistryVersionStore Versions)> CreateContributorAsync(
        IReadOnlyCollection<IdentityProviderConnection> configuredConnections,
        params IdentityProviderConnection[] databaseConnections) =>
        CreateContributorAsync(configuredConnections, databaseConnections, null);

    private static async Task<(ExternalAuthenticationRoleDeletionDependencyContributor Contributor, InMemoryIdentityProviderConnectionStore Store, InMemoryConnectionRegistryVersionStore Versions)> CreateContributorAsync(
        IReadOnlyCollection<IdentityProviderConnection> configuredConnections,
        IdentityProviderConnection[] databaseConnections,
        IReadOnlyCollection<Role>? additionalRoles = null,
        ITenantAccessor? tenantAccessor = null,
        Func<InMemoryIdentityProviderConnectionStore, IIdentityProviderConnectionStore>? decorateStore = null)
    {
        var store = new InMemoryIdentityProviderConnectionStore();
        foreach (var connection in databaseConnections)
            await Assert.That(await store.CreateAsync(connection)).IsOfType(typeof(ConnectionMutationResult.Created));

        var accessor = tenantAccessor ?? TestTenantAccessor.Default;
        var roleStore = new MemoryRoleStore(new MemoryStore<Role>(), accessor);
        await roleStore.SaveAsync(new Role { Id = "workflow-user", Name = "Workflow user", TenantId = accessor.TenantId, Permissions = [] });
        await roleStore.SaveAsync(new Role { Id = "other-role", Name = "Other role", TenantId = accessor.TenantId, Permissions = [] });
        foreach (var role in additionalRoles ?? [])
            await roleStore.SaveAsync(role);
        var versions = new InMemoryConnectionRegistryVersionStore();
        var services = new ServiceCollection().BuildServiceProvider();
        var contributor = new ExternalAuthenticationRoleDeletionDependencyContributor(
            decorateStore?.Invoke(store) ?? store,
            new MutableOptionsMonitor<ExternalAuthenticationOptions>(new ExternalAuthenticationOptions { ConfigurationConnections = configuredConnections.ToList() }),
            [new RoleAuthorizationService(new StoreBasedRoleProvider(roleStore), new PermissionEvaluator())],
            [roleStore],
            versions,
            new ConnectionRevisionCalculator(),
            new ExternalAuthenticationSecurityNotifier(services),
            new PermissionEvaluator(),
            accessor);
        return (contributor, store, versions);
    }

    private static IdentityProviderConnection Connection(string id, PolicySelection policy, string? tenantId = null) => new()
    {
        Id = id,
        TenantId = tenantId ?? ConnectionScope.HostTenantId,
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

    private static PolicySelection CreateUserPolicy(params string[] defaultRoleIds) => new(
        CreateUserUnlinkedIdentityPolicy.PolicyType,
        1,
        JsonSerializer.SerializeToElement(new { defaultRoleIds }));

    private static async Task AssertDefaultRoleIds(IdentityProviderConnection? connection, params string[] expectedRoleIds)
    {
        var identityProviderConnection = await Assert.That(connection).IsNotNull();
        await Assert.That(identityProviderConnection).IsOfType(typeof(IdentityProviderConnection));
        var actualRoleIds = identityProviderConnection.UnlinkedPolicy!.Settings.GetProperty("defaultRoleIds").EnumerateArray().Select(x => x.GetString()!).ToArray();
        await Assert.That(actualRoleIds).IsEquivalentTo(expectedRoleIds, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
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

    /// <summary>
    /// Reassigns a connection to another tenant once the contributor has read it, which puts the connection
    /// outside the role's tenant context between prevalidation and the remediation write.
    /// </summary>
    private sealed class ConnectionStoreThatMovesConnectionToAnotherTenant(
        InMemoryIdentityProviderConnectionStore inner,
        string connectionId,
        string tenantId,
        int lookupsBeforeMove) : IIdentityProviderConnectionStore
    {
        private int _lookups;

        public ValueTask<Page<IdentityProviderConnection>> FindAsync(ConnectionFilter filter, CancellationToken cancellationToken = default) =>
            inner.FindAsync(filter, cancellationToken);

        public async ValueTask<IdentityProviderConnection?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var connection = await inner.FindByIdAsync(id, cancellationToken);
            if (connection is not null && string.Equals(id, connectionId, StringComparison.Ordinal) && Interlocked.Increment(ref _lookups) > lookupsBeforeMove)
                connection.TenantId = tenantId;
            return connection;
        }

        public ValueTask<ConnectionMutationResult> CreateAsync(IdentityProviderConnection connection, CancellationToken cancellationToken = default) =>
            inner.CreateAsync(connection, cancellationToken);

        public ValueTask<ConnectionMutationResult> UpdateAsync(IdentityProviderConnection connection, long expectedRevision, CancellationToken cancellationToken = default) =>
            inner.UpdateAsync(connection, expectedRevision, cancellationToken);
    }

    /// <summary>
    /// Resolves roles by ID alone, regardless of the ambient tenant, standing in for the EF Core role store
    /// with multitenancy disabled: it installs no tenant query filter and can resolve a tenant-owned role by
    /// ID no matter which tenant is ambient. <see cref="MemoryRoleStore"/> cannot exercise that scenario
    /// because it always filters by the ambient tenant itself.
    /// </summary>
    private sealed class RoleStoreWithoutAmbientTenantFilter(IReadOnlyCollection<Role> roles) : IRoleStore
    {
        public Task AddAsync(Role role, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SaveAsync(Role role, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(roles.FirstOrDefault(x => x.Id == filter.Id));

        public Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(roles.Where(x => x.Id == filter.Id));
    }

    private sealed class RoleStoreThatRemovesReplacementAfterContributorValidation(
        MemoryRoleStore inner,
        string replacementRoleId) : IRoleStore
    {
        private int _replacementChecks;

        public bool ReplacementRemoved { get; private set; }

        public Task AddAsync(Role role, CancellationToken cancellationToken = default) => inner.AddAsync(role, cancellationToken);

        public Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);

        public Task SaveAsync(Role role, CancellationToken cancellationToken = default) => inner.SaveAsync(role, cancellationToken);

        public Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);

        public async Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default)
        {
            var roles = (await inner.FindManyAsync(filter, cancellationToken)).ToArray();
            if (!ReplacementRemoved && filter.Ids?.Contains(replacementRoleId) == true && Interlocked.Increment(ref _replacementChecks) == 2)
            {
                await inner.DeleteAsync(new() { Id = replacementRoleId }, cancellationToken);
                ReplacementRemoved = true;
            }

            return roles;
        }
    }
}
