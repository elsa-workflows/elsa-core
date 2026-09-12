using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Elsa.UserTasks.Persistence.ConformanceTests.Providers;

namespace Elsa.UserTasks.Persistence.ConformanceTests;

/// <summary>
/// The behaviour every <see cref="IUserTaskRepository"/> owes its callers, run unchanged against each
/// provider. The contract is what callers depend on; a provider that satisfies it only in memory is a
/// provider that fails in production, which is exactly how the revision-conflict defect reached review.
/// </summary>
public abstract class UserTaskRepositoryConformanceTests(UserTaskStoreFixture fixture) : UserTaskConformanceTestBase(fixture)
{
    [ConformanceFact]
    public async Task AStaleSaveThrowsTheContractsConflictNotTheStoresNativeConcurrencyType()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));

        var first = await GetAsync(task.Id);
        var second = await GetAsync(task.Id);
        first.Priority = 90;
        await Repository.SaveAsync(first, first.Revision);

        second.Priority = 10;
        // Assert.ThrowsAsync matches the exact type, so this also pins that the store's own concurrency
        // exception does not escape: DbUpdateConcurrencyException and DocumentStoreConcurrencyException
        // would both fail here, which is precisely the defect that shipped a 500 instead of a 409.
        var conflict = await Assert.ThrowsAsync<UserTaskRevisionConflictException>(() => Repository.SaveAsync(second, second.Revision));

        Assert.Equal(task.Id, conflict.TaskId);
        Assert.Equal(second.Revision, conflict.ExpectedRevision);
    }

    [ConformanceFact]
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
        await Assert.ThrowsAsync<UserTaskRevisionConflictException>(() => other.SaveAsync(theirs, theirs.Revision));

        var settled = await GetAsync(task.Id);
        Assert.Equal(UserTaskStatus.Assigned, settled.Status);
        Assert.Equal(task.Revision + 1, settled.Revision);
    }

    [ConformanceFact]
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

        Assert.False(mutated);
        Assert.Equal(1, (await GetAsync(task.Id)).Priority);
    }

    [ConformanceFact]
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
        Assert.False(mutated);
        Assert.Equal(task.Priority, settled.Priority);
        // A declined mutation must not consume the revision either, or the caller's next command would
        // fail with a conflict it has no way to explain.
        Assert.Equal(task.Revision, settled.Revision);
    }

    [ConformanceFact]
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
        Assert.Equal(revision, audited.Revision);
        Assert.Equal(2, audited.Events.Count(x => x.Revision == revision));

        // The revision the caller was already holding still commits.
        audited.Priority = 42;
        await Repository.SaveAsync(audited, revision);
        Assert.Equal(42, (await GetAsync(task.Id)).Priority);
    }

    [ConformanceFact]
    public async Task AppendEventIgnoresAnUnknownTask()
    {
        await ActivateAsync();
        var task = CreateTask(Subject());

        // Never projected. Auditing something that no longer exists is a lost race, not a fault.
        await Repository.AppendEventAsync(TenantId, task.Id, Event(task, 1, "Viewed", "event-orphan"));

        Assert.Null(await Repository.GetAsync(TenantId, task.Id));
    }

    [ConformanceFact]
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
        Assert.Equal(1, all.TotalCount);
        Assert.Equal(task.Id, Assert.Single(all.Items).Id);
        Assert.Null(await Repository.GetAsync(TenantId, replay.Id));
    }

    [ConformanceFact]
    public async Task LookupsByMaterializationKeyAndBookmarkAreTenantScoped()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));

        Assert.Equal(task.Id, (await Repository.FindByMaterializationKeyAsync(TenantId, task.MaterializationKey))?.Id);
        Assert.Equal(task.Id, (await Repository.FindByBookmarkIdAsync(TenantId, task.BookmarkId))?.Id);
        Assert.Null(await Repository.FindByMaterializationKeyAsync("other-tenant", task.MaterializationKey));
        Assert.Null(await Repository.FindByBookmarkIdAsync("other-tenant", task.BookmarkId));
        Assert.Null(await Repository.GetAsync("other-tenant", task.Id));
    }

    [ConformanceFact]
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

        Assert.NotNull(match);
        var resolved = match!.Value;
        Assert.Equal(task.Id, resolved.Task.Id);
        Assert.Equal(TenantId, resolved.Task.TenantId);
        Assert.Equal("Complete", Assert.Single(resolved.Invitation.AllowedActions));
        Assert.Null(await Repository.FindByInvitationTokenHashAsync($"HASH-UNKNOWN-{Guid.NewGuid():N}"));
    }

    [ConformanceFact]
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
        Assert.Equal(2, page.TotalCount);
        Assert.Equal([visible.Id, alsoVisible.Id], page.Items.Select(x => x.Id).Order(StringComparer.Ordinal));

        // The same must hold once a page limit forces a cursor: the excluded rows cannot occupy a slot.
        var paged = await PageThroughAsync(Query(), pageSize: 1);
        Assert.Equal([visible.Id, alsoVisible.Id], paged.Order(StringComparer.Ordinal));
    }

    [ConformanceFact]
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

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [ConformanceTheory]
    [InlineData("created", false)]
    [InlineData("created", true)]
    [InlineData("due", false)]
    [InlineData("due", true)]
    [InlineData("priority", false)]
    [InlineData("priority", true)]
    [InlineData("title", false)]
    [InlineData("title", true)]
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
            Assert.Equal(expected, await PageThroughAsync(query, pageSize));
    }

    [ConformanceFact]
    public async Task TasksWithoutADueDateOrderLastInBothDirections()
    {
        await ActivateAsync();
        await SeedSortableTasksAsync();

        foreach (var descending in new[] { false, true })
        {
            var page = await Repository.QueryAsync(Query(sort: "due", descending: descending, limit: 200));
            var dueDates = page.Items.Select(x => x.DueAt).ToList();
            var firstNull = dueDates.FindIndex(x => x is null);

            Assert.NotEqual(-1, firstNull);
            // Once the nulls start they must not be interrupted, in either direction. A generic numeric
            // comparison reorders them and the cursor then drops or repeats rows at the boundary.
            Assert.All(dueDates.Skip(firstNull), x => Assert.Null(x));
        }
    }

    [ConformanceFact]
    public async Task ThePageLimitIsHonouredAndTheFinalPageReportsNoCursor()
    {
        await ActivateAsync();
        await SeedSortableTasksAsync();

        var first = await Repository.QueryAsync(Query(limit: 2));
        Assert.Equal(2, first.Items.Count);
        Assert.NotNull(first.NextCursor);

        var last = await Repository.QueryAsync(Query(limit: 200));
        Assert.Equal(SortableTaskCount, last.Items.Count);
        // A cursor on a page that already returned everything sends the caller round again for nothing.
        Assert.Null(last.NextCursor);
    }

    [ConformanceFact]
    public async Task AnUnreadableCursorIsIgnoredRatherThanFailingTheRequest()
    {
        await ActivateAsync();
        await SeedSortableTasksAsync();

        var result = await Repository.QueryAsync(Query(limit: 200, cursor: "not-a-cursor"));

        Assert.Equal(SortableTaskCount, result.Items.Count);
    }

    private const int SortableTaskCount = 6;

    /// <summary>
    /// Seeds a set that exercises every sort key at once: distinct titles, distinct priorities, a mix of
    /// present and absent due dates, and two rows sharing a due date so the identity tiebreaker is used.
    /// </summary>
    private async Task SeedSortableTasksAsync()
    {
        var subject = Subject();
        var baseline = Clock.UtcNow;
        var shared = baseline.AddDays(3);
        UserTask[] tasks =
        [
            CreateTask(subject, "Alpha", priority: 10, dueAt: baseline.AddDays(1)),
            CreateTask(subject, "Bravo", priority: 90, dueAt: shared),
            CreateTask(subject, "Charlie", priority: 50, dueAt: shared),
            CreateTask(subject, "Delta", priority: 30, dueAt: baseline.AddDays(5)),
            CreateTask(subject, "Echo", priority: 70, dueAt: null),
            CreateTask(subject, "Foxtrot", priority: 20, dueAt: null)
        ];

        foreach (var task in tasks)
            await Repository.AddProjectionAsync(task);
    }

    private UserTaskEvent Event(UserTask task, int revision, string type, string id) =>
        new($"{id}-{Guid.NewGuid():N}", TenantId, task.Id, revision, type, Clock.UtcNow);
}
