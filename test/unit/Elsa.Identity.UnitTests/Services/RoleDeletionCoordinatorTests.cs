using System.Security.Claims;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;

namespace Elsa.Identity.UnitTests.Services;

public class RoleDeletionCoordinatorTests
{
    [Fact]
    public async Task InspectionRequiresDeleteRolePermission()
    {
        var (_, coordinator) = await CreateCoordinatorAsync(new StubContributor([]));

        var result = await coordinator.InspectAsync("workflow-user", new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.IsType<RoleDeletionInspectionResult.Forbidden>(result);
    }

    [Fact]
    public async Task OrdinaryDeletionIsBlockedByConfigurationDependency()
    {
        var (store, coordinator) = await CreateCoordinatorAsync(
            new StubContributor([
                Dependency("configuration", RoleDeletionDependencyOwnership.Configuration, configurationPath: "ExternalAuthentication:Connections:0:UnlinkedPolicy:Settings:defaultRoleIds:0")
            ]));

        var result = await coordinator.DeleteAsync("workflow-user", Administrator());

        Assert.IsType<RoleDeletionOperationResult.Blocked>(result);
        Assert.NotNull(await store.FindAsync(new() { Id = "workflow-user" }));
    }

    [Fact]
    public async Task BestEffortRemediationRequiresAllConfirmations()
    {
        var contributor = new StubContributor([Dependency("connection-a", removesLastDefaultRole: true)], reportsAtomicRemoval: true);
        var (_, coordinator) = await CreateCoordinatorAsync(contributor);
        var impact = Assert.IsType<RoleDeletionInspectionResult.Success>(await coordinator.InspectAsync("workflow-user", Administrator())).Impact;

        Assert.Equal(RoleDeletionExecutionMode.BestEffort, impact.ExecutionMode);

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            false,
            false,
            false));

        var confirmation = Assert.IsType<RoleDeletionOperationResult.ConfirmationRequired>(result);
        Assert.Equal(
            ["confirm_remove_from_editable_jit_policies", "removes_last_default_role", "confirm_best_effort"],
            confirmation.Warnings);
    }

    [Fact]
    public async Task SuccessfulRemediationRemovesDependenciesBeforeDeletingRole()
    {
        var contributor = new StubContributor([Dependency("connection-a", removesLastDefaultRole: true)]);
        var (store, coordinator) = await CreateCoordinatorAsync(contributor);
        var impact = Assert.IsType<RoleDeletionInspectionResult.Success>(await coordinator.InspectAsync("workflow-user", Administrator())).Impact;

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            true,
            true,
            true));

        var deleted = Assert.IsType<RoleDeletionOperationResult.Deleted>(result);
        Assert.Equal(["connection-a"], deleted.ChangedOwnerIds);
        Assert.Null(await store.FindAsync(new() { Id = "workflow-user" }));
        Assert.Empty(contributor.Dependencies);
    }

    [Fact]
    public async Task IncompleteBestEffortRemediationLeavesRoleIntact()
    {
        var contributor = new StubContributor(
            [Dependency("connection-a"), Dependency("connection-b")],
            failAfterFirst: true);
        var (store, coordinator) = await CreateCoordinatorAsync(contributor);
        var impact = Assert.IsType<RoleDeletionInspectionResult.Success>(await coordinator.InspectAsync("workflow-user", Administrator())).Impact;

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            true,
            true,
            true));

        var incomplete = Assert.IsType<RoleDeletionOperationResult.Incomplete>(result);
        Assert.Equal(["connection-a"], incomplete.ChangedOwnerIds);
        Assert.NotNull(await store.FindAsync(new() { Id = "workflow-user" }));
        Assert.Single(contributor.Dependencies);
    }

    private static async Task<(MemoryRoleStore Store, RoleDeletionCoordinator Coordinator)> CreateCoordinatorAsync(IRoleDeletionDependencyContributor contributor)
    {
        var store = new MemoryRoleStore(new MemoryStore<Role>());
        await store.SaveAsync(new Role { Id = "workflow-user", Name = "Workflow user", Permissions = [] });
        var roleProvider = new StoreBasedRoleProvider(store);
        var coordinator = new RoleDeletionCoordinator(store, new RoleAuthorizationService(roleProvider), [contributor]);
        return (store, coordinator);
    }

    private static ClaimsPrincipal Administrator() => new(new ClaimsIdentity([new Claim(PermissionNames.ClaimType, PermissionNames.All)]));

    private static RoleDeletionDependency Dependency(
        string ownerId,
        RoleDeletionDependencyOwnership ownership = RoleDeletionDependencyOwnership.Database,
        string? configurationPath = null,
        bool removesLastDefaultRole = false) => new(
        StubContributor.SourceName,
        ownerId,
        ownerId,
        "create-user",
        ownership,
        configurationPath,
        ownership == RoleDeletionDependencyOwnership.Database ? 1 : null,
        removesLastDefaultRole);

    private sealed class StubContributor(
        IReadOnlyCollection<RoleDeletionDependency> dependencies,
        bool failAfterFirst = false,
        bool reportsAtomicRemoval = false) : IRoleDeletionDependencyContributor
    {
        public const string SourceName = "test";
        public string Source => SourceName;
        public IReadOnlyCollection<RoleDeletionDependency> Dependencies { get; private set; } = dependencies;

        public ValueTask<RoleDeletionDependencySnapshot> InspectAsync(string roleId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new RoleDeletionDependencySnapshot(Source, Version(), reportsAtomicRemoval, Dependencies));

        public ValueTask<RoleReferenceRemovalValidationResult> ValidateRemovalAsync(RoleReferenceRemovalRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<RoleReferenceRemovalValidationResult>(
                string.Equals(request.ExpectedContributorVersion, Version(), StringComparison.Ordinal)
                    ? new RoleReferenceRemovalValidationResult.Valid()
                    : new RoleReferenceRemovalValidationResult.Conflict("changed"));

        public ValueTask<RoleReferenceRemovalResult> RemoveEditableReferencesAsync(RoleReferenceRemovalRequest request, CancellationToken cancellationToken = default)
        {
            if (failAfterFirst)
            {
                var changed = Dependencies.OrderBy(x => x.OwnerId, StringComparer.Ordinal).First();
                Dependencies = Dependencies.Where(x => !string.Equals(x.OwnerId, changed.OwnerId, StringComparison.Ordinal)).ToArray();
                return ValueTask.FromResult<RoleReferenceRemovalResult>(new RoleReferenceRemovalResult.Failed("simulated", [changed.OwnerId]));
            }

            var changedOwnerIds = Dependencies.Select(x => x.OwnerId).ToArray();
            Dependencies = [];
            return ValueTask.FromResult<RoleReferenceRemovalResult>(new RoleReferenceRemovalResult.Success(changedOwnerIds));
        }

        private string Version() => string.Join("|", Dependencies.Select(x => $"{x.OwnerId}:{x.ExpectedRevision}"));
    }
}
