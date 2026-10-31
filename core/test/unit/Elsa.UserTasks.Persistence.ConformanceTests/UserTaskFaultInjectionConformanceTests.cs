using Elsa.Authorization;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Persistence.ConformanceTests.Faults;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Elsa.UserTasks.Persistence.ConformanceTests.Providers;
using Elsa.UserTasks.Permissions;
using Elsa.UserTasks.Services;
using Elsa.Workflows;

namespace Elsa.UserTasks.Persistence.ConformanceTests;

/// <summary>
/// Runs the real <see cref="DefaultUserTaskManager"/> and <see cref="DefaultUserTaskInvitationService"/>
/// against a real store, with failures injected at the seams between them.
///
/// Every P1 defect in the User Tasks build was found this way and none by a happy-path test, so injecting
/// faults is treated here as a first-class part of the contract rather than a special case: a cross-store
/// operation must either commit fully or leave the caller able to retry, and a retry must converge.
/// </summary>
public abstract class UserTaskFaultInjectionConformanceTests(UserTaskStoreFixture fixture) : UserTaskConformanceTestBase(fixture)
{
    private readonly TestIdentityGenerator _identity = new();
    private readonly TestSink _sink = new();
    private readonly DefaultUserTaskAccessPolicy _policy = new();

    [ConformanceFact]
    public async Task AConcurrentEditMakesTheManagerReportAConflictInsteadOfFaulting()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));
        var manager = CreateManager(Repository);

        // Someone else moves the task on, so the revision the caller holds is now stale.
        var concurrent = await GetAsync(task.Id);
        concurrent.Priority = 90;
        await Repository.SaveAsync(concurrent, concurrent.Revision);

        var result = await manager.ClaimAsync(TenantId, task.Id, new(task.Revision, "claim-1"), Actor());

        // The store raises its own concurrency exception underneath. It must surface as the documented
        // conflict result rather than escaping to the unhandled-error middleware as a 500.
        Assert.False(result.Accepted);
        Assert.Equal("revision-conflict", result.ConflictCode);
    }

    [ConformanceFact]
    public async Task TwoCompletionsOnOneRevisionLeaveExactlyOneWinnerAndOneConflict()
    {
        await ActivateAsync();
        var subject = Subject();
        var task = await ProjectAsync(CreateTask(subject));
        var actor = Actor();
        var manager = CreateManager(Repository);
        var contender = CreateManager(Fixture.CreateSecondRepository());

        var claimed = await manager.ClaimAsync(TenantId, task.Id, new(task.Revision, "claim-1"), actor);
        Assert.True(claimed.Accepted);
        var revision = claimed.Task!.Revision;

        var first = await manager.CompleteAsync(TenantId, task.Id, new(revision, "op-first", "Complete"), actor);
        var second = await contender.CompleteAsync(TenantId, task.Id, new(revision, "op-second", "Complete"), actor);

        Assert.True(first.Accepted);
        Assert.False(second.Accepted);
        // The loser gets a conflict it can act on, never an unhandled exception and never a silent success.
        Assert.Equal("revision-conflict", second.ConflictCode);
        // Completion is two-phase: the manager records the intent and the workflow resumption settles it.
        // With no resumer attached, Completing is the committed state, and only one of the two got there.
        Assert.Equal(UserTaskStatus.Completing, (await GetAsync(task.Id)).Status);
    }

    [ConformanceFact]
    public async Task AFailureInThePreCommitSweepLeavesTheInvitationRevocableAndTheRetryRepairsIt()
    {
        await ActivateAsync();
        var (task, invitationId, credential, sessions) = await IssueGuestSessionAsync();
        var invitations = CreateInvitationService(Repository, sessions);
        sessions.ResetCounters();

        // Fail the sweep that runs before anything is committed.
        sessions.FailRevokeForInvitationWhen = ordinal => ordinal == 1;
        var current = await GetAsync(task.Id);
        await Assert.ThrowsAsync<InjectedStoreFaultException>(() =>
            invitations.RevokeAsync(TenantId, task.Id, invitationId, current.Revision, ManagerActor()));

        // Nothing was committed, so the invitation is still open and the credential is still live. Failing
        // closed here is the point: committing the terminal state first would strand a live credential
        // behind a guard that rejects the retry.
        var afterFailure = await GetAsync(task.Id);
        Assert.Equal(UserTaskInvitationStatus.Consumed, Invitation(afterFailure, invitationId).Status);
        Assert.NotNull(await sessions.ResolveAsync(credential));

        sessions.FailRevokeForInvitationWhen = null;
        Assert.True(await invitations.RevokeAsync(TenantId, task.Id, invitationId, afterFailure.Revision, ManagerActor()));
        Assert.Equal(UserTaskInvitationStatus.Revoked, Invitation(await GetAsync(task.Id), invitationId).Status);
        Assert.Null(await sessions.ResolveAsync(credential));
    }

    [ConformanceFact]
    public async Task AFailureInThePostCommitSweepIsRepairedIdempotentlyByARetry()
    {
        await ActivateAsync();
        var (task, invitationId, credential, sessions) = await IssueGuestSessionAsync();
        var invitations = CreateInvitationService(Repository, sessions);
        sessions.ResetCounters();

        // Let the pre-commit sweep run and fail the one after the commit, so the aggregate reads Revoked
        // while a session issued in the commit window could still be live.
        sessions.FailRevokeForInvitationWhen = ordinal => ordinal == 2;
        var current = await GetAsync(task.Id);
        await Assert.ThrowsAsync<InjectedStoreFaultException>(() =>
            invitations.RevokeAsync(TenantId, task.Id, invitationId, current.Revision, ManagerActor()));
        Assert.Equal(UserTaskInvitationStatus.Revoked, Invitation(await GetAsync(task.Id), invitationId).Status);

        // The retry finds an already-revoked invitation. It must report success and sweep again rather than
        // reporting a failure the caller cannot act on and leaving the credential behind.
        sessions.FailRevokeForInvitationWhen = null;
        var sweepsBefore = sessions.RevokeForInvitationCallCount;
        var afterFailure = await GetAsync(task.Id);
        Assert.True(await invitations.RevokeAsync(TenantId, task.Id, invitationId, afterFailure.Revision, ManagerActor()));

        Assert.True(sessions.RevokeForInvitationCallCount > sweepsBefore);
        Assert.Null(await sessions.ResolveAsync(credential));
    }

    [ConformanceFact]
    public async Task ASuccessfulRevocationSweepsSessionsOnBothSidesOfTheCommit()
    {
        await ActivateAsync();
        var (task, invitationId, credential, sessions) = await IssueGuestSessionAsync();
        var invitations = CreateInvitationService(Repository, sessions);
        sessions.ResetCounters();

        var current = await GetAsync(task.Id);
        Assert.True(await invitations.RevokeAsync(TenantId, task.Id, invitationId, current.Revision, ManagerActor()));

        // Both sweeps are load-bearing: the first keeps a store failure from committing, the second catches
        // a session a concurrent verification issued between the first sweep and the commit.
        Assert.Equal(2, sessions.RevokeForInvitationCallCount);
        Assert.Null(await sessions.ResolveAsync(credential));
    }

    [ConformanceFact]
    public async Task ACredentialIssuedInsideTheRevokeCommitWindowIsDeadEitherWay()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));
        var sessions = new FaultingGuestSessionIssuer(Fixture.GuestSessions);
        var dispatcher = new CapturingDispatcher();
        var invitations = CreateInvitationService(Repository, sessions);

        var issued = await invitations.IssueAsync(TenantId, task.Id, new(task.Revision, "bearer", ["Complete"]), ManagerActor());
        Assert.NotNull(issued);
        await DrainOutboxAsync(dispatcher);

        // Revoke from inside IssueAsync, so the revocation runs after the guest session lands in the store
        // but before verification re-reads the settled invitation state.
        sessions.AfterIssue = async () =>
        {
            var current = await GetAsync(task.Id);
            await invitations.RevokeAsync(TenantId, task.Id, issued!.Invitation.Id, current.Revision, ManagerActor());
        };

        var verified = await invitations.VerifyAsync(new(dispatcher.Token!));

        // Whichever side wins, no live credential may survive a successful revoke.
        Assert.False(verified.Succeeded);
        Assert.Equal("invitation-unavailable", verified.FailureCode);
        Assert.Null(verified.SessionToken);
    }

    [ConformanceFact]
    public async Task AFailedSaveCommitsNothingAndTheRetrySucceedsOnTheSameRevision()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));
        var faulting = new FaultingUserTaskRepository(Repository) { FailSaveCalls = 1 };

        var attempt = await GetAsync(task.Id);
        attempt.Priority = 77;
        await Assert.ThrowsAsync<InjectedStoreFaultException>(() => faulting.SaveAsync(attempt, task.Revision));

        // A store that fails must leave the aggregate exactly as it was, revision included, or the retry
        // the caller is about to make would come back as a conflict it cannot explain.
        var afterFailure = await GetAsync(task.Id);
        Assert.Equal(task.Priority, afterFailure.Priority);
        Assert.Equal(task.Revision, afterFailure.Revision);

        await faulting.SaveAsync(attempt, task.Revision);
        Assert.Equal(2, faulting.SaveCallCount);
        Assert.Equal(77, (await GetAsync(task.Id)).Priority);
    }

    [ConformanceFact]
    public async Task AFailedCompareAndSwapCommitsNothingAndTheRetryConverges()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));
        var faulting = new FaultingUserTaskRepository(Repository) { FailTryMutateCalls = 1 };
        var mutation = (UserTask current) =>
        {
            current.Status = UserTaskStatus.Cancelled;
            return true;
        };

        await Assert.ThrowsAsync<InjectedStoreFaultException>(() => faulting.TryMutateAsync(TenantId, task.Id, task.Revision, mutation));
        Assert.Equal(task.Status, (await GetAsync(task.Id)).Status);

        Assert.True(await faulting.TryMutateAsync(TenantId, task.Id, task.Revision, mutation));
        Assert.Equal(2, faulting.TryMutateCallCount);
        Assert.Equal(UserTaskStatus.Cancelled, (await GetAsync(task.Id)).Status);
    }

    [ConformanceFact]
    public async Task AnAuditWriteFailureDoesNotConsumeTheCallersRevision()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));
        var faulting = new FaultingUserTaskRepository(Repository) { FailAppendEventCalls = 1 };

        await Assert.ThrowsAsync<InjectedStoreFaultException>(() =>
            faulting.AppendEventAsync(TenantId, task.Id, new($"event-{Guid.NewGuid():N}", TenantId, task.Id, task.Revision, "Viewed", Clock.UtcNow)));

        // The command the caller was already holding a revision for still commits.
        var held = await GetAsync(task.Id);
        held.Priority = 33;
        await Repository.SaveAsync(held, task.Revision);
        Assert.Equal(33, (await GetAsync(task.Id)).Priority);
    }

    private async Task<(UserTask Task, string InvitationId, string Credential, FaultingGuestSessionIssuer Sessions)> IssueGuestSessionAsync()
    {
        var task = await ProjectAsync(CreateTask(Subject()));
        var sessions = new FaultingGuestSessionIssuer(Fixture.GuestSessions);
        var dispatcher = new CapturingDispatcher();
        var invitations = CreateInvitationService(Repository, sessions);

        var issued = await invitations.IssueAsync(TenantId, task.Id, new(task.Revision, "bearer", ["Complete"]), ManagerActor())
                     ?? throw new InvalidOperationException("The invitation could not be issued.");
        await DrainOutboxAsync(dispatcher);

        var verified = await invitations.VerifyAsync(new(dispatcher.Token!));
        if (!verified.Succeeded)
            throw new InvalidOperationException($"The invitation could not be verified: {verified.FailureCode}.");

        return (await GetAsync(task.Id), issued.Invitation.Id, verified.SessionToken!, sessions);
    }

    private async Task DrainOutboxAsync(CapturingDispatcher dispatcher)
    {
        foreach (var delivery in await Fixture.Outbox.DequeueDueAsync(100))
        {
            await dispatcher.DispatchAsync(delivery);
            await Fixture.Outbox.CompleteAsync(delivery.Id);
        }
    }

    private static UserTaskInvitation Invitation(UserTask task, string invitationId) =>
        task.Invitations.Single(x => x.Id == invitationId);

    private DefaultUserTaskManager CreateManager(IUserTaskRepository repository) =>
        new(repository, _policy, [], new NoOpResumer(), _sink, _identity, Clock, Fixture.Options);

    private DefaultUserTaskInvitationService CreateInvitationService(IUserTaskRepository repository, IUserTaskGuestSessionIssuer sessions) =>
        new(repository, _policy, Fixture.Outbox, new DefaultUserTaskInvitationVerifier(), sessions, _sink, _identity, Clock, Fixture.Options);

    /// <summary>The named verbs, as permissions on the <c>user-tasks</c> resource.</summary>
    private static IReadOnlySet<string> Grants(params string[] verbs) =>
        verbs.Select(verb => new Permission(UserTasksResourcePermissions.UserTasks, verb).ToString()).ToHashSet(StringComparer.Ordinal);

    private UserTaskActor Actor(string id = "user-1") => new(Subject(id), [])
    {
        Permissions = Grants(CoreVerbs.View, UserTaskVerbs.Claim, UserTaskVerbs.Complete)
    };

    private UserTaskActor ManagerActor() => Actor("manager-1") with
    {
        IsManager = true,
        Permissions = Grants(CoreVerbs.View, UserTaskVerbs.Claim, UserTaskVerbs.Complete, UserTaskVerbs.Assign,
            CoreVerbs.Update, UserTaskVerbs.Cancel, UserTaskVerbs.Invite, UserTaskVerbs.Supervise)
    };

    private sealed class TestIdentityGenerator : IIdentityGenerator
    {
        private int _counter;
        public string GenerateId() => $"id-{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}";
    }

    private sealed class NoOpResumer : IUserTaskWorkflowResumer
    {
        public Task ResumeAsync(UserTask task, UserTaskStimulus stimulus, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestSink : IUserTaskNotificationSink
    {
        public Task PublishAsync(UserTaskLifecycleNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CapturingDispatcher : IUserTaskInvitationDispatcher
    {
        public string? Token { get; private set; }

        public Task DispatchAsync(UserTaskInvitationDelivery delivery, CancellationToken cancellationToken = default)
        {
            Token = delivery.Token;
            return Task.CompletedTask;
        }
    }
}
