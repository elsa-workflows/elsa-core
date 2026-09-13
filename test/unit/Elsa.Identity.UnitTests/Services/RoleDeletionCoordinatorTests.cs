using Elsa.Testing.Shared.Multitenancy;
using Elsa.Authorization;
using System.Security.Claims;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Notifications;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Mediator.Contracts;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Services;

public class RoleDeletionCoordinatorTests
{
    [Test]
    public async Task RoleRemediationContractsRetainTheirLegacyConstructors()
    {
        var removalConstructor = typeof(RoleReferenceRemovalRequest).GetConstructor([
            typeof(string),
            typeof(ClaimsPrincipal),
            typeof(string),
            typeof(IReadOnlyCollection<RoleDeletionDependency>)]);
        var commandConstructor = typeof(RoleDeletionRemediationCommand).GetConstructor([
            typeof(string),
            typeof(ClaimsPrincipal),
            typeof(string),
            typeof(bool),
            typeof(bool),
            typeof(bool)]);

        await Assert.That(removalConstructor).IsNotNull();
        await Assert.That(commandConstructor).IsNotNull();
        await Assert.That(typeof(RoleReferenceRemovalRequest).GetConstructors()).HasSingleItem();
        await Assert.That(typeof(RoleDeletionRemediationCommand).GetConstructors()).HasSingleItem();
        await Assert.That(typeof(RoleReferenceRemovalRequest).GetProperty(nameof(RoleReferenceRemovalRequest.SelectedReferences))).IsNotNull();
        await Assert.That(typeof(RoleReferenceRemovalRequest).GetProperty(nameof(RoleReferenceRemovalRequest.ReplacementRoleId))).IsNotNull();
        await Assert.That(typeof(RoleDeletionRemediationCommand).GetProperty(nameof(RoleDeletionRemediationCommand.SelectedReferences))).IsNotNull();
        await Assert.That(typeof(RoleDeletionRemediationCommand).GetProperty(nameof(RoleDeletionRemediationCommand.ReplacementRoleId))).IsNotNull();
    }

    [Test]
    public async Task InspectionRequiresDeleteRolePermission()
    {
        var (_, coordinator) = await CreateCoordinatorAsync(new StubContributor([]));

        var result = await coordinator.InspectAsync("workflow-user", new ClaimsPrincipal(new ClaimsIdentity()));

        await Assert.That(result).IsOfType(typeof(RoleDeletionInspectionResult.Forbidden));
    }

    [Test]
    [Arguments("identity/roles:delete")] // the permission the delete endpoints declare
    [Arguments("identity/*:delete")]     // a subtree grant that reaches it
    [Arguments("identity/roles:*")]      // a verb wildcard on the resource
    public async Task InspectionAcceptsTheStructuredDeletePermission(string grant)
    {
        // Every other test here acts as an administrator holding "*", which is why this went unnoticed: the
        // coordinator compared claim values against the legacy string "delete:role", and nothing has granted
        // that since the vocabulary migration. A caller holding identity/roles:delete passed the endpoint's
        // own check and was then refused here, so role deletion worked only for holders of "*".
        var (_, coordinator) = await CreateCoordinatorAsync(new StubContributor([]));

        var result = await coordinator.InspectAsync("workflow-user", PrincipalWith(grant));

        await Assert.That(result).IsOfType(typeof(RoleDeletionInspectionResult.Success));
    }

    [Test]
    public async Task InspectionStillRefusesAnUnrelatedPermission()
    {
        var (_, coordinator) = await CreateCoordinatorAsync(new StubContributor([]));

        var result = await coordinator.InspectAsync("workflow-user", PrincipalWith("identity/roles:view"));

        await Assert.That(result).IsOfType(typeof(RoleDeletionInspectionResult.Forbidden));
    }

    [Test]
    public async Task OrdinaryDeletionIsBlockedByConfigurationDependency()
    {
        var notificationSender = Substitute.For<INotificationSender>();
        var (store, coordinator) = await CreateCoordinatorAsync(
            new StubContributor([
                Dependency("configuration", RoleDeletionDependencyOwnership.Configuration, configurationPath: "ExternalAuthentication:Connections:0:UnlinkedPolicy:Settings:defaultRoleIds:0")
            ]),
            notificationSender);

        var result = await coordinator.DeleteAsync("workflow-user", Administrator());

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.Blocked));
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNotNull();
        await AssertNoRoleNotificationAsync(notificationSender);
    }

    [Test]
    public async Task SuccessfulDeletionPublishesDeletedRoleNotification()
    {
        var notificationSender = Substitute.For<INotificationSender>();
        var (store, coordinator) = await CreateCoordinatorAsync(new StubContributor([]), notificationSender);
        // A cancellable request token is what makes the token assertion below mean anything: were the notification
        // published with the request's own token, that token could be cancelled and the assertion would fail.
        using var request = new CancellationTokenSource();

        var result = await coordinator.DeleteAsync("workflow-user", Administrator(), request.Token);

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.Deleted));
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNull();
        await AssertRoleDeletedNotificationAsync(notificationSender);
    }

    [Test]
    public async Task DeletionThatRemovedNothingReportsNotFoundWithoutPublishing()
    {
        // What the loser of a race sees: the role is still there to be found, and the delete then removes no row
        // because a concurrent request got there first. Publishing here would credit this request with a deletion
        // it did not perform, and audit would record the role as deleted twice.
        var notificationSender = Substitute.For<INotificationSender>();
        var (_, coordinator) = await CreateCoordinatorAsync(
            new StubContributor([]),
            notificationSender,
            inner => new RoleStoreThatDeletesNothing(inner));

        var result = await coordinator.DeleteAsync("workflow-user", Administrator());

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.NotFound));
        await AssertNoRoleNotificationAsync(notificationSender);
    }

    [Test]
    public async Task DeletionThroughStoreWithoutAtomicCapabilityStillPublishesOnce()
    {
        // A third-party store implementing only IRoleStore reports no affected-row count. The deletion must still
        // reach security subscribers over that legacy path rather than being dropped for want of the capability.
        var notificationSender = Substitute.For<INotificationSender>();
        var (store, coordinator) = await CreateCoordinatorAsync(
            new StubContributor([]),
            notificationSender,
            inner => new LegacyRoleStore(inner));

        var result = await coordinator.DeleteAsync("workflow-user", Administrator());

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.Deleted));
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNull();
        await AssertRoleDeletedNotificationAsync(notificationSender);
    }

    [Test]
    public async Task ConcurrentDeletionsPublishExactlyOneNotification()
    {
        // Both requests are held until each has already found the role, so neither can be turned away by the
        // existence check and the store's own delete is the only thing that can separate them.
        var notificationSender = Substitute.For<INotificationSender>();
        var (store, coordinator) = await CreateCoordinatorAsync(
            new StubContributor([]),
            notificationSender,
            inner => new RoleStoreThatDeletesInLockstep(inner, 2));

        var results = await Task.WhenAll(
            Task.Run(async () => await coordinator.DeleteAsync("workflow-user", Administrator())),
            Task.Run(async () => await coordinator.DeleteAsync("workflow-user", Administrator())));

        await Assert.That(results).HasSingleItem(result => result is RoleDeletionOperationResult.Deleted);
        await Assert.That(results).HasSingleItem(result => result is RoleDeletionOperationResult.NotFound);
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNull();
        await AssertRoleDeletedNotificationAsync(notificationSender);
    }

    [Test]
    public async Task BestEffortRemediationRequiresAllConfirmations()
    {
        var contributor = new StubContributor([Dependency("connection-a", removesLastDefaultRole: true)], reportsAtomicRemoval: true);
        var (_, coordinator) = await CreateCoordinatorAsync(contributor);
        var inspection = await coordinator.InspectAsync("workflow-user", Administrator());
        await Assert.That(inspection).IsOfType(typeof(RoleDeletionInspectionResult.Success));
        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;

        await Assert.That(impact.ExecutionMode).IsEqualTo(RoleDeletionExecutionMode.BestEffort);

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            false,
            false,
            false));

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.ConfirmationRequired));
        var confirmation = (RoleDeletionOperationResult.ConfirmationRequired)result;
        await Assert.That(confirmation.Warnings).IsEquivalentTo(
            ["confirm_remove_from_editable_jit_policies", "removes_last_default_role", "confirm_best_effort"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task SuccessfulRemediationRemovesDependenciesBeforeDeletingRole()
    {
        var contributor = new StubContributor([Dependency("connection-a", removesLastDefaultRole: true)]);
        var notificationSender = Substitute.For<INotificationSender>();
        var (store, coordinator) = await CreateCoordinatorAsync(contributor, notificationSender);
        var inspection = await coordinator.InspectAsync("workflow-user", Administrator());
        await Assert.That(inspection).IsOfType(typeof(RoleDeletionInspectionResult.Success));
        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            true,
            true,
            true));

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.Deleted));
        var deleted = (RoleDeletionOperationResult.Deleted)result;
        await Assert.That(deleted.ChangedOwnerIds).IsEquivalentTo(["connection-a"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNull();
        await Assert.That(contributor.Dependencies).IsEmpty();
        await AssertRoleDeletedNotificationAsync(notificationSender);
    }

    [Test]
    public async Task IncompleteBestEffortRemediationLeavesRoleIntact()
    {
        var contributor = new StubContributor(
            [Dependency("connection-a"), Dependency("connection-b")],
            failAfterFirst: true);
        var notificationSender = Substitute.For<INotificationSender>();
        var (store, coordinator) = await CreateCoordinatorAsync(contributor, notificationSender);
        var inspection = await coordinator.InspectAsync("workflow-user", Administrator());
        await Assert.That(inspection).IsOfType(typeof(RoleDeletionInspectionResult.Success));
        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            true,
            true,
            true));

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.Incomplete));
        var incomplete = (RoleDeletionOperationResult.Incomplete)result;
        await Assert.That(incomplete.ChangedOwnerIds).IsEquivalentTo(["connection-a"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNotNull();
        await Assert.That(contributor.Dependencies).HasSingleItem();
        await AssertNoRoleNotificationAsync(notificationSender);
    }

    [Test]
    public async Task SelectiveRemediationChangesOnlySelectedDependenciesAndRetainsRoleWhenOthersRemain()
    {
        var contributor = new StubContributor([Dependency("connection-a"), Dependency("connection-b")]);
        var (store, coordinator) = await CreateCoordinatorAsync(contributor);
        var inspection = await coordinator.InspectAsync("workflow-user", Administrator());
        await Assert.That(inspection).IsOfType(typeof(RoleDeletionInspectionResult.Success));
        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            true,
            true,
            true)
        {
            SelectedReferences = [new RoleDeletionReferenceSelection(StubContributor.SourceName, "connection-a")]
        });

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.Incomplete));
        var incomplete = (RoleDeletionOperationResult.Incomplete)result;
        await Assert.That(incomplete.ChangedOwnerIds).IsEquivalentTo(["connection-a"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNotNull();
        await Assert.That(contributor.Dependencies.Select(x => x.OwnerId).ToArray()).IsEquivalentTo(["connection-b"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task ExplicitEmptySelectionDoesNotMutateAndReturnsIncomplete()
    {
        var contributor = new StubContributor([Dependency("connection-a")]);
        var (store, coordinator) = await CreateCoordinatorAsync(contributor);
        var inspection = await coordinator.InspectAsync("workflow-user", Administrator());
        await Assert.That(inspection).IsOfType(typeof(RoleDeletionInspectionResult.Success));
        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            true,
            true,
            true)
        {
            SelectedReferences = []
        });

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.Incomplete));
        var incomplete = (RoleDeletionOperationResult.Incomplete)result;
        await Assert.That(incomplete.ChangedOwnerIds).IsEmpty();
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNotNull();
        await Assert.That(contributor.Dependencies).HasSingleItem();
    }

    [Test]
    public async Task SelectedFinalDefaultRequiresCoordinatorValidatedReplacementBeforeMutation()
    {
        var contributor = new StubContributor([Dependency("connection-a", removesLastDefaultRole: true)]);
        var (store, coordinator) = await CreateCoordinatorAsync(contributor);
        var inspection = await coordinator.InspectAsync("workflow-user", Administrator());
        await Assert.That(inspection).IsOfType(typeof(RoleDeletionInspectionResult.Success));
        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            true,
            true,
            true)
        {
            SelectedReferences = [new RoleDeletionReferenceSelection(StubContributor.SourceName, "connection-a")],
            ReplacementRoleId = "replacement-role"
        });

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.ValidationFailed));
        var validation = (RoleDeletionOperationResult.ValidationFailed)result;
        await Assert.That(validation.Code).IsEqualTo("replacement_role_not_found");
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNotNull();
        await Assert.That(contributor.Dependencies).HasSingleItem();
    }

    [Test]
    public async Task ConfigurationDependencyBlocksSelectiveDatabaseRemediation()
    {
        var contributor = new StubContributor([
            Dependency("configuration", RoleDeletionDependencyOwnership.Configuration, configurationPath: "ExternalAuthentication:Connections:0"),
            Dependency("connection-a")
        ]);
        var (store, coordinator) = await CreateCoordinatorAsync(contributor);
        var inspection = await coordinator.InspectAsync("workflow-user", Administrator());
        await Assert.That(inspection).IsOfType(typeof(RoleDeletionInspectionResult.Success));
        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            true,
            true,
            true)
        {
            SelectedReferences = [new RoleDeletionReferenceSelection(StubContributor.SourceName, "connection-a")]
        });

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.Blocked));
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNotNull();
        await Assert.That(contributor.Dependencies.Count).IsEqualTo(2);
    }

    [Test]
    [Arguments("unknown", "connection-a", "unknown_reference")]
    [Arguments(StubContributor.SourceName, "connection-a", "duplicate_reference")]
    public async Task InvalidSelectionFailsClosedWithoutMutation(string source, string ownerId, string expectedCode)
    {
        var contributor = new StubContributor([Dependency("connection-a")]);
        var (store, coordinator) = await CreateCoordinatorAsync(contributor);
        var inspection = await coordinator.InspectAsync("workflow-user", Administrator());
        await Assert.That(inspection).IsOfType(typeof(RoleDeletionInspectionResult.Success));
        var impact = ((RoleDeletionInspectionResult.Success)inspection).Impact;
        var selections = expectedCode == "duplicate_reference"
            ? new[]
            {
                new RoleDeletionReferenceSelection(source, ownerId),
                new RoleDeletionReferenceSelection(source, ownerId)
            }
            : new[] { new RoleDeletionReferenceSelection(source, ownerId) };

        var result = await coordinator.RemediateAndDeleteAsync(new(
            "workflow-user",
            Administrator(),
            impact.DependencyVersion,
            true,
            true,
            true)
        {
            SelectedReferences = selections
        });

        await Assert.That(result).IsOfType(typeof(RoleDeletionOperationResult.ValidationFailed));
        var validation = (RoleDeletionOperationResult.ValidationFailed)result;
        await Assert.That(validation.Code).IsEqualTo(expectedCode);
        await Assert.That(await store.FindAsync(new() { Id = "workflow-user" })).IsNotNull();
        await Assert.That(contributor.Dependencies).HasSingleItem();
    }

    private static async Task<(MemoryRoleStore Store, RoleDeletionCoordinator Coordinator)> CreateCoordinatorAsync(
        IRoleDeletionDependencyContributor contributor,
        INotificationSender? notificationSender = null,
        Func<MemoryRoleStore, IRoleStore>? storeDecorator = null)
    {
        var store = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        await store.SaveAsync(new Role { Id = "workflow-user", Name = "Workflow user", Permissions = [] });
        var roleStore = storeDecorator?.Invoke(store) ?? store;
        var roleProvider = new StoreBasedRoleProvider(roleStore);
        var securityNotifier = new RoleSecurityNotifier(notificationSender ?? Substitute.For<INotificationSender>(), TestTenantAccessor.Default, new SystemClock());
        var coordinator = new RoleDeletionCoordinator(roleStore, new RoleAuthorizationService(roleProvider, new PermissionEvaluator()), [contributor], securityNotifier);
        return (store, coordinator);
    }

    private static async Task AssertRoleDeletedNotificationAsync(INotificationSender notificationSender) =>
        await notificationSender.Received(1).SendAsync(
            Arg.Is<RoleChanged>(notification =>
                notification.Operation == "deleted" &&
                notification.RoleId == "workflow-user" &&
                notification.RoleName == "Workflow user" &&
                notification.Permissions.Count == 0),
            // The row is already gone by the time this is published, so a request that is cancelled or abandoned
            // must not be able to silence it. An uncancellable token is how that is guaranteed.
            Arg.Is<CancellationToken>(token => !token.CanBeCanceled));

    private static async Task AssertNoRoleNotificationAsync(INotificationSender notificationSender) =>
        await notificationSender.DidNotReceive().SendAsync(Arg.Any<RoleChanged>(), Arg.Any<CancellationToken>());

    private static ClaimsPrincipal Administrator() => new(new ClaimsIdentity([new Claim(PermissionNames.ClaimType, PermissionNames.All)]));

    private static ClaimsPrincipal PrincipalWith(string permission) => new(new ClaimsIdentity([new Claim(PermissionNames.ClaimType, permission)]));

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
            var selectedOwnerIds = request.Dependencies.Select(x => x.OwnerId).ToHashSet(StringComparer.Ordinal);
            if (failAfterFirst)
            {
                var changed = Dependencies.Where(x => selectedOwnerIds.Contains(x.OwnerId)).OrderBy(x => x.OwnerId, StringComparer.Ordinal).First();
                Dependencies = Dependencies.Where(x => !string.Equals(x.OwnerId, changed.OwnerId, StringComparison.Ordinal)).ToArray();
                return ValueTask.FromResult<RoleReferenceRemovalResult>(new RoleReferenceRemovalResult.Failed("simulated", [changed.OwnerId]));
            }

            var changedOwnerIds = Dependencies.Where(x => selectedOwnerIds.Contains(x.OwnerId)).Select(x => x.OwnerId).ToArray();
            Dependencies = Dependencies.Where(x => !selectedOwnerIds.Contains(x.OwnerId)).ToArray();
            return ValueTask.FromResult<RoleReferenceRemovalResult>(new RoleReferenceRemovalResult.Success(changedOwnerIds));
        }

        private string Version() => string.Join("|", Dependencies.Select(x => $"{x.OwnerId}:{x.ExpectedRevision}"));
    }

    /// <summary>Offers only <see cref="IRoleStore"/>, standing in for a store that predates atomic deletion.</summary>
    private class LegacyRoleStore(MemoryRoleStore inner) : IRoleStore
    {
        protected MemoryRoleStore Inner { get; } = inner;

        public Task AddAsync(Role role, CancellationToken cancellationToken = default) => Inner.AddAsync(role, cancellationToken);

        public Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default) => Inner.DeleteAsync(filter, cancellationToken);

        public Task SaveAsync(Role role, CancellationToken cancellationToken = default) => Inner.SaveAsync(role, cancellationToken);

        public Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default) => Inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default) => Inner.FindManyAsync(filter, cancellationToken);
    }

    /// <summary>Finds the role but reports that the delete removed nothing, as the loser of a race would.</summary>
    private sealed class RoleStoreThatDeletesNothing(MemoryRoleStore inner) : LegacyRoleStore(inner), IRoleStoreWithAtomicDelete
    {
        public Task<bool> TryDeleteAsync(string roleId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    /// <summary>Holds every caller at the delete until they have all found the role, then lets them race for real.</summary>
    private sealed class RoleStoreThatDeletesInLockstep(MemoryRoleStore inner, int callers) : LegacyRoleStore(inner), IRoleStoreWithAtomicDelete
    {
        private readonly Barrier _barrier = new(callers);

        public Task<bool> TryDeleteAsync(string roleId, CancellationToken cancellationToken = default)
        {
            if (!_barrier.SignalAndWait(TimeSpan.FromSeconds(30)))
                throw new TimeoutException("The concurrent deletions never met at the barrier.");

            return Inner.TryDeleteAsync(roleId, cancellationToken);
        }
    }
}