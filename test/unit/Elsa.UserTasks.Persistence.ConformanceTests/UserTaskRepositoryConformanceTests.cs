using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Elsa.UserTasks.Persistence.ConformanceTests.Providers;
using System.Threading.Tasks;

namespace Elsa.UserTasks.Persistence.ConformanceTests;

/// <summary>
/// The behaviour every <see cref="IUserTaskRepository"/> owes its callers, run unchanged against each
/// provider. The contract is what callers depend on; a provider that satisfies it only in memory is a
/// provider that fails in production, which is exactly how the revision-conflict defect reached review.
/// </summary>
public abstract class UserTaskRepositoryConformanceTests(UserTaskStoreFixture fixture) : UserTaskConformanceTestBase(fixture)
{
    [Test]
    public async Task AStaleSaveThrowsTheContractsConflictNotTheStoresNativeConcurrencyType()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));

        var first = await GetAsync(task.Id);
        var second = await GetAsync(task.Id);
        first.Priority = 90;
        await Repository.SaveAsync(first, first.Revision);

        second.Priority = 10;
        // ThrowsExactly matches the exact type, so this also pins that the store's own concurrency
        // exception does not escape: DbUpdateConcurrencyException and DocumentStoreConcurrencyException
        // would both fail here, which is precisely the defect that shipped a 500 instead of a 409.
        var conflict = await Assert.That(() => Repository.SaveAsync(second, second.Revision)).ThrowsExactly<UserTaskRevisionConflictException>();

        await Assert.That(conflict.TaskId).IsEqualTo(task.Id);
        await Assert.That(conflict.ExpectedRevision).IsEqualTo(second.Revision);
    }

    [Test]
    public async Task TwoWritersOnSeparateConnectionsLeaveExactlyOneWinnerAndOneConflict()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));
        var other = Fixture.CreateSecondRepository();

        var mine = await GetAsync(task.Id);
        var theirs = await other.GetAsync(TenantId, task.Id) ?? throw new InvalidOperationException("The second connection could not read the task.");
        mine.Status = UserTaskStatus.Assigned;
        theirs.Status = UserTaskStatus.Cancelled;

        await Repository.SaveAsync(mine, mine.Revision);
        await Assert.That(() => other.SaveAsync(theirs, theirs.Revision)).ThrowsExactly<UserTaskRevisionConflictException>();

        var settled = await GetAsync(task.Id);
        await Assert.That(settled.Status).IsEqualTo(UserTaskStatus.Assigned);
        await Assert.That(settled.Revision).IsEqualTo(task.Revision + 1);
    }

    [Test]
    public async Task TryMutateReturnsFalseOnALostRaceRatherThanThrowing()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));
        var staleRevision = task.Revision;

        var winner = await GetAsync(task.Id);
        winner.Priority = 1;
        await Repository.SaveAsync(winner, winner.Revision);

        var mutated = await Repository.TryMutateAsync(TenantId, task.Id, staleRevision, current =>
        {
            current.Priority = 99;
            return true;
        });

        await Assert.That(mutated).IsFalse();
        await Assert.That((await GetAsync(task.Id)).Priority).IsEqualTo(1);
    }

    [Test]
    public async Task TryMutateCommitsNothingWhenTheMutationDeclines()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));

        var mutated = await Repository.TryMutateAsync(TenantId, task.Id, task.Revision, current =>
        {
            current.Priority = 7;
            return false;
        });

        var settled = await GetAsync(task.Id);
        await Assert.That(mutated).IsFalse();
        await Assert.That(settled.Priority).IsEqualTo(task.Priority);
        // A declined mutation must not consume the revision either, or the caller's next command would
        // fail with a conflict it has no way to explain.
        await Assert.That(settled.Revision).IsEqualTo(task.Revision);
    }

    [Test]
    public async Task AppendEventDoesNotConsumeTheConcurrencyToken()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));
        var revision = task.Revision;

        // Several entries deliberately share one revision: audit is append-only and an audited read must
        // not invalidate an expected revision a client is already holding.
        await Repository.AppendEventAsync(TenantId, task.Id, Event(task, revision, "Viewed", "event-a"));
        await Repository.AppendEventAsync(TenantId, task.Id, Event(task, revision, "FieldRevealed", "event-b"));

        var audited = await GetAsync(task.Id);
        await Assert.That(audited.Revision).IsEqualTo(revision);
        await Assert.That(audited.Events.Count(x => x.Revision == revision)).IsEqualTo(2);

        // The revision the caller was already holding still commits.
        audited.Priority = 42;
        await Repository.SaveAsync(audited, revision);
        await Assert.That((await GetAsync(task.Id)).Priority).IsEqualTo(42);
    }

    [Test]
    public async Task AppendEventIgnoresAnUnknownTask()
    {
        await ActivateAsync();
        var task = CreateTask(Subject());

        // Never projected. Auditing something that no longer exists is a lost race, not a fault.
        await Repository.AppendEventAsync(TenantId, task.Id, Event(task, 1, "Viewed", "event-orphan"));

        await Assert.That(await Repository.GetAsync(TenantId, task.Id)).IsNull();
    }

    [Test]
    public async Task AddProjectionIsIdempotentOnTheMaterializationKey()
    {
        await ActivateAsync();
        var task = CreateTask(Subject());
        await Repository.AddProjectionAsync(task);

        // A redelivered bookmark commit replays the same materialization key under a different task id.
        var replay = CreateTask(Subject());
        replay.MaterializationKey = task.MaterializationKey;
        await Repository.AddProjectionAsync(replay);

        var all = await Repository.QueryAsync(Query(UserTaskQueryScopeKind.Available, includeTotalCount: true));
        await Assert.That(all.TotalCount).IsEqualTo(1);
        var onlyTask = await Assert.That(all.Items).HasSingleItem();
        await Assert.That(onlyTask.Id).IsEqualTo(task.Id);
        await Assert.That(await Repository.GetAsync(TenantId, replay.Id)).IsNull();
    }

    [Test]
    public async Task LookupsByMaterializationKeyAndBookmarkAreTenantScoped()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));

        await Assert.That((await Repository.FindByMaterializationKeyAsync(TenantId, task.MaterializationKey))?.Id).IsEqualTo(task.Id);
        await Assert.That((await Repository.FindByBookmarkIdAsync(TenantId, task.BookmarkId))?.Id).IsEqualTo(task.Id);
        await Assert.That(await Repository.FindByMaterializationKeyAsync("other-tenant", task.MaterializationKey)).IsNull();
        await Assert.That(await Repository.FindByBookmarkIdAsync("other-tenant", task.BookmarkId)).IsNull();
        await Assert.That(await Repository.GetAsync("other-tenant", task.Id)).IsNull();
    }

    [Test]
    public async Task InvitationLookupResolvesFromATokenHashAloneAndReturnsNullForUnknown()
    {
        await ActivateAsync();
        var task = CreateTask(Subject());
        var tokenHash = $"HASH-{Guid.NewGuid():N}";
        task.Invitations.Add(new("invitation-1", TenantId, task.Id, "guest@example.com", tokenHash,
            UserTaskInvitationStatus.Pending, Clock.UtcNow, Clock.UtcNow.AddDays(1), "bearer")
        {
            AllowedActions = ["Complete"]
        });
        await Repository.AddProjectionAsync(task);

        // Deliberately tenant-agnostic: an anonymous holder presents only a secret and must never be
        // trusted to name its own tenant.
        var match = await Repository.FindByInvitationTokenHashAsync(tokenHash);

        var resolved = await Assert.That(match).IsNotNull();
        await Assert.That(resolved.Task.Id).IsEqualTo(task.Id);
        await Assert.That(resolved.Task.TenantId).IsEqualTo(TenantId);
        var onlyAction = await Assert.That(resolved.Invitation.AllowedActions).HasSingleItem();
        await Assert.That(onlyAction).IsEqualTo("Complete");
        await Assert.That(await Repository.FindByInvitationTokenHashAsync($"HASH-UNKNOWN-{Guid.NewGuid():N}")).IsNull();
    }

    [Test]
    public async Task SafeSearchByTagReturnsOnlyTheMatchingTask()
    {
        await ActivateAsync();
        var subject = Subject();

        var tagged = CreateTask(subject, title: "Approve invoice");
        tagged.Tags = ["priority-escalation", "routine-review"];
        await Repository.AddProjectionAsync(tagged);

        var other = CreateTask(subject, title: "Approve invoice");
        other.Tags = ["routine-review"];
        await Repository.AddProjectionAsync(other);

        var page = await Repository.QueryAsync(Query(includeTotalCount: true) with
        {
            Search = "priority-escalation"
        });

        await Assert.That(page.TotalCount).IsEqualTo(1);
        var onlyPageTask = await Assert.That(page.Items).HasSingleItem();
        await Assert.That(onlyPageTask.Id).IsEqualTo(tagged.Id);

        var upper = await Repository.QueryAsync(Query(includeTotalCount: true) with
        {
            Search = "PRIORITY-ESCALATION"
        });
        var onlyUpperTask = await Assert.That(upper.Items).HasSingleItem();
        await Assert.That(onlyUpperTask.Id).IsEqualTo(tagged.Id);

        // JSON array syntax sits between tags in EF storage. That text is not a tag value, so
        // InMemory/VNext reject it and EF must not treat the serialized payload as a match.
        var jsonSyntax = await Repository.QueryAsync(Query(includeTotalCount: true) with
        {
            Search = """priority-escalation","routine-review"""
        });
        await Assert.That(jsonSyntax.Items).IsEmpty();
        await Assert.That(jsonSyntax.TotalCount).IsEqualTo(0);

        var punctuated = CreateTask(subject, title: "Approve invoice");
        punctuated.Tags = ["review[urgent]"];
        await Repository.AddProjectionAsync(punctuated);

        var bracket = await Repository.QueryAsync(Query(includeTotalCount: true) with
        {
            Search = "review[urgent]"
        });
        var onlyBracketTask = await Assert.That(bracket.Items).HasSingleItem();
        await Assert.That(onlyBracketTask.Id).IsEqualTo(punctuated.Id);
    }

    [Test]
    public async Task ScopeAndExclusionApplyBeforeTotalsCursorsAndPageLimits()
    {
        await ActivateAsync();
        var subject = Subject();
        var visible = await ProjectAsync(CreateTask(subject, title: "Visible one"));
        var alsoVisible = await ProjectAsync(CreateTask(subject, title: "Visible two"));

        var excluded = CreateTask(subject, title: "Excluded");
        excluded.ExcludedUsers = [subject];
        await Repository.AddProjectionAsync(excluded);

        var foreign = CreateTask(Subject("someone-else"), title: "Not a candidate");
        await Repository.AddProjectionAsync(foreign);

        var page = await Repository.QueryAsync(Query(includeTotalCount: true));

        // The unauthorized rows are absent from the total, not merely hidden on the page. A count that
        // includes them leaks their existence and pushes authorized rows off the last page.
        await Assert.That(page.TotalCount).IsEqualTo(2);
        await Assert.That(page.Items.Select(x => x.Id).Order(StringComparer.Ordinal)).IsEquivalentTo([visible.Id, alsoVisible.Id], TUnit.Assertions.Enums.CollectionOrdering.Matching);

        // The same must hold once a page limit forces a cursor: the excluded rows cannot occupy a slot.
        var paged = await PageThroughAsync(Query(), pageSize: 1);
        await Assert.That(paged.Order(StringComparer.Ordinal)).IsEquivalentTo([visible.Id, alsoVisible.Id], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task AvailableScopeShowsASnapshotMemberOnlyWhenTheTaskUsesSnapshotMode()
    {
        await ActivateAsync();
        var alice = Subject("alice");
        var other = Subject("other");

        var snapshotVisible = CreateTask(title: "Snapshot member");
        snapshotVisible.MembershipResolutionMode = UserTaskMembershipResolutionMode.Snapshot;
        snapshotVisible.SnapshotMembers = [alice];
        snapshotVisible.CandidateUsers = [other];
        await Repository.AddProjectionAsync(snapshotVisible);

        var liveHidden = CreateTask(title: "Live ignores leftover snapshot members");
        liveHidden.MembershipResolutionMode = UserTaskMembershipResolutionMode.Live;
        liveHidden.SnapshotMembers = [alice];
        liveHidden.CandidateUsers = [other];
        await Repository.AddProjectionAsync(liveHidden);

        var page = await Repository.QueryAsync(Query(includeTotalCount: true, subject: alice));

        await Assert.That(page.TotalCount).IsEqualTo(1);
        var onlyTask = await Assert.That(page.Items).HasSingleItem();
        await Assert.That(onlyTask.Id).IsEqualTo(snapshotVisible.Id);
    }

    [Test]
    public async Task AvailableScopeShowsALiveCandidateOnlyWhenTheTaskUsesLiveMode()
    {
        await ActivateAsync();
        var alice = Subject("alice");
        var other = Subject("other");

        var liveVisible = CreateTask(title: "Live candidate");
        liveVisible.MembershipResolutionMode = UserTaskMembershipResolutionMode.Live;
        liveVisible.CandidateUsers = [alice];
        liveVisible.SnapshotMembers = [other];
        await Repository.AddProjectionAsync(liveVisible);

        var snapshotHidden = CreateTask(title: "Snapshot ignores leftover live candidates");
        snapshotHidden.MembershipResolutionMode = UserTaskMembershipResolutionMode.Snapshot;
        snapshotHidden.CandidateUsers = [alice];
        snapshotHidden.SnapshotMembers = [other];
        await Repository.AddProjectionAsync(snapshotHidden);

        var page = await Repository.QueryAsync(Query(includeTotalCount: true, subject: alice));

        await Assert.That(page.TotalCount).IsEqualTo(1);
        var onlyTask = await Assert.That(page.Items).HasSingleItem();
        await Assert.That(onlyTask.Id).IsEqualTo(liveVisible.Id);
    }

    [Test]
    public async Task AvailableScopeDoesNotTreatSnapshotGroupsAsLiveMembership()
    {
        await ActivateAsync();
        var alice = Subject("alice");
        var reviewers = Group("reviewers");

        var snapshot = CreateTask(title: "Snapshot group without enumerated member");
        snapshot.MembershipResolutionMode = UserTaskMembershipResolutionMode.Snapshot;
        snapshot.CandidateGroups = [reviewers];
        snapshot.SnapshotGroups = [reviewers];
        await Repository.AddProjectionAsync(snapshot);

        var page = await Repository.QueryAsync(Query(includeTotalCount: true, subject: alice) with
        {
            Scope = new(TenantId, alice, [reviewers], Kind: UserTaskQueryScopeKind.Available)
        });

        await Assert.That(page.Items).IsEmpty();
        await Assert.That(page.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task AvailableScopeStillAppliesExclusionsBeforeSnapshotMembership()
    {
        await ActivateAsync();
        var alice = Subject("alice");

        var excluded = CreateTask(title: "Excluded snapshot member");
        excluded.MembershipResolutionMode = UserTaskMembershipResolutionMode.Snapshot;
        excluded.SnapshotMembers = [alice];
        excluded.ExcludedUsers = [alice];
        await Repository.AddProjectionAsync(excluded);

        var page = await Repository.QueryAsync(Query(includeTotalCount: true, subject: alice));

        await Assert.That(page.Items).IsEmpty();
        await Assert.That(page.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task AScopeFromAnotherTenantMatchesNothingEvenWhenTheQueryNamesThisOne()
    {
        await ActivateAsync();
        await ProjectAsync(CreateTask(Subject()));

        var crossTenant = Query() with
        {
            Scope = new("other-tenant", Subject() with { TenantId = "other-tenant" }, [], Kind: UserTaskQueryScopeKind.Available),
            IncludeTotalCount = true
        };
        var result = await Repository.QueryAsync(crossTenant);

        await Assert.That(result.Items).IsEmpty();
        await Assert.That(result.TotalCount).IsEqualTo(0);
    }

    [Test]
    [ConformanceCursorCases]
    public async Task CursorsAreStableAcrossEverySupportedSortAndDirection(string sort, bool descending)
    {
        await ActivateAsync();
        await SeedSortableTasksAsync();

        var query = Query(sort: sort, descending: descending);
        var unpaged = await Repository.QueryAsync(query with { Limit = 200 });
        var expected = unpaged.Items.Select(x => x.Id).ToList();

        // Paging must reproduce the unpaged order exactly: no row seen twice, none skipped, and no
        // dependence on the page size. A cursor that only works at one limit is not a cursor.
        foreach (var pageSize in new[] { 1, 2, 3 })
            await Assert.That(await PageThroughAsync(query, pageSize)).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task UpdatedSortUsesUpdatedAtThenAscendingId()
    {
        await ActivateAsync();
        await SeedSortableTasksAsync();

        // Seed UpdatedAt order is Bravo, Foxtrot, Charlie+Delta (shared, Id tie), Echo, Alpha —
        // not the created/title order — so a provider that still maps updated to created fails.
        var ascending = await Repository.QueryAsync(Query(sort: "updated", limit: 200));
        await Assert.That(ascending.Items.Select(x => x.Title)).IsEquivalentTo(["Bravo", "Foxtrot", "Charlie", "Delta", "Echo", "Alpha"], TUnit.Assertions.Enums.CollectionOrdering.Matching);

        var descending = await Repository.QueryAsync(Query(sort: "updated", descending: true, limit: 200));
        await Assert.That(descending.Items.Select(x => x.Title)).IsEquivalentTo(["Alpha", "Echo", "Charlie", "Delta", "Foxtrot", "Bravo"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task TasksWithoutADueDateOrderLastInBothDirections()
    {
        await ActivateAsync();
        await SeedSortableTasksAsync();

        foreach (var descending in new[] { false, true })
        {
            var page = await Repository.QueryAsync(Query(sort: "due", descending: descending, limit: 200));
            var dueDates = page.Items.Select(x => x.DueAt).ToList();
            var firstNull = dueDates.FindIndex(x => x is null);

            await Assert.That(firstNull).IsNotEqualTo(-1);
            // Once the nulls start they must not be interrupted, in either direction. A generic numeric
            // comparison reorders them and the cursor then drops or repeats rows at the boundary.
            await Assert.That(dueDates.Skip(firstNull)).All(x => x == null);
        }
    }

    [Test]
    public async Task ThePageLimitIsHonouredAndTheFinalPageReportsNoCursor()
    {
        await ActivateAsync();
        await SeedSortableTasksAsync();

        var first = await Repository.QueryAsync(Query(limit: 2));
        await Assert.That(first.Items.Count).IsEqualTo(2);
        await Assert.That(first.NextCursor).IsNotNull();

        var last = await Repository.QueryAsync(Query(limit: 200));
        await Assert.That(last.Items.Count).IsEqualTo(SortableTaskCount);
        // A cursor on a page that already returned everything sends the caller round again for nothing.
        await Assert.That(last.NextCursor).IsNull();
    }

    [Test]
    public async Task AnUnreadableCursorIsIgnoredRatherThanFailingTheRequest()
    {
        await ActivateAsync();
        await SeedSortableTasksAsync();

        var result = await Repository.QueryAsync(Query(limit: 200, cursor: "not-a-cursor"));

        await Assert.That(result.Items.Count).IsEqualTo(SortableTaskCount);
    }

    private const int SortableTaskCount = 6;

    /// <summary>
    /// Seeds a set that exercises every sort key at once: distinct titles, distinct priorities, a mix of
    /// present and absent due dates, updated times that are not the created order, and two rows sharing a
    /// due date and two sharing an updated time so the identity tiebreaker is used.
    /// </summary>
    private async Task SeedSortableTasksAsync()
    {
        var subject = Subject();
        var baseline = Clock.UtcNow;
        var shared = baseline.AddDays(3);
        var sharedUpdated = baseline.AddHours(3);
        UserTask[] tasks =
        [
            CreateTask(subject, "Alpha", priority: 10, dueAt: baseline.AddDays(1), updatedAt: baseline.AddHours(6)),
            CreateTask(subject, "Bravo", priority: 90, dueAt: shared, updatedAt: baseline.AddHours(1)),
            CreateTask(subject, "Charlie", priority: 50, dueAt: shared, updatedAt: sharedUpdated),
            CreateTask(subject, "Delta", priority: 30, dueAt: baseline.AddDays(5), updatedAt: sharedUpdated),
            CreateTask(subject, "Echo", priority: 70, dueAt: null, updatedAt: baseline.AddHours(5)),
            CreateTask(subject, "Foxtrot", priority: 20, dueAt: null, updatedAt: baseline.AddHours(2))
        ];

        foreach (var task in tasks)
            await Repository.AddProjectionAsync(task);
    }

    private UserTaskEvent Event(UserTask task, int revision, string type, string id) =>
        new($"{id}-{Guid.NewGuid():N}", TenantId, task.Id, revision, type, Clock.UtcNow);
}
