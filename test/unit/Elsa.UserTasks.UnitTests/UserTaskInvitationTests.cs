using System.Text.Json;
using Elsa.Authorization;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Permissions;
using Elsa.UserTasks.Services;

namespace Elsa.UserTasks.UnitTests;

/// <summary>
/// Covers the invitation and guest-session boundary: one-time secrets, generic anonymous responses, rate
/// limiting, task-scoped guest authorization, and revocation.
/// </summary>
public class UserTaskInvitationTests
{
    private const string Tenant = UserTaskTestFixture.TenantId;

    private readonly UserTaskTestFixture _fixture = new();

    private static Func<UserTaskDefinitionSnapshot, UserTaskDefinitionSnapshot> WithBearerInvitation(params string[] actions) =>
        UserTaskTestFixture.WithBearerInvitation(actions);

    [Test]
    public async Task Issue_KeepsTheSecretOutOfTheApiResultAndOffTheAuditTrail()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());

        var issued = await Assert.That(await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Approve"]), manager)).IsNotNull();

        await _fixture.DrainOutboxAsync();
        var token = await Assert.That(_fixture.Dispatcher.Token).IsNotNull();
        await Assert.That(JsonSerializer.Serialize(issued)).DoesNotContain(token).WithComparison(StringComparison.CurrentCulture);

        var stored = await _fixture.Repository.GetAsync(Tenant, task.Id);
        await Assert.That(JsonSerializer.Serialize(stored!.Events)).DoesNotContain(token).WithComparison(StringComparison.CurrentCulture);
        await Assert.That(JsonSerializer.Serialize(stored.Invitations)).DoesNotContain(token).WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Issue_RefusesToBroadenAnInvitationBeyondTheActivityDefinition()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation("Approve"));

        // "Reject" is a configured task action but was not part of the materialized invitation definition.
        await Assert.That(await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Approve", "Reject"]), manager)).IsNull();
        await Assert.That(await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Reject"]), manager)).IsNull();
    }

    [Test]
    public async Task Issue_RequiresTheInvitePermissionAndManagerRelationship()
    {
        var participant = _fixture.Actor("user-1", UserTaskTestFixture.Grant(CoreVerbs.View), UserTaskTestFixture.Grant(UserTaskVerbs.Invite));
        var task = await _fixture.ProjectAsync(participant.Subject, WithBearerInvitation());

        await Assert.That(await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Approve"]), participant)).IsNull();
    }

    [Test]
    public async Task Verify_ConsumesTheWinningInvitationAndRevokesItsSiblings()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());

        await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(1, "bearer", ["Approve"]), manager);
        await _fixture.DrainOutboxAsync();
        var first = _fixture.Dispatcher.Token!;
        await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(2, "bearer", ["Approve"]), manager);
        await _fixture.DrainOutboxAsync();
        var second = _fixture.Dispatcher.Token!;

        var verified = await _fixture.Invitations.VerifyAsync(new(first));
        await Assert.That(verified.Succeeded).IsTrue();
        await Assert.That(verified.TaskId).IsEqualTo(task.Id);
        await Assert.That(verified.SessionToken).IsNotNull();

        // The sibling and a replay of the winner are both rejected, with the same opaque code.
        var sibling = await _fixture.Invitations.VerifyAsync(new(second));
        var replay = await _fixture.Invitations.VerifyAsync(new(first));
        await Assert.That(sibling.Succeeded).IsFalse();
        await Assert.That(replay.Succeeded).IsFalse();
        await Assert.That(sibling.FailureCode).IsEqualTo("invitation-unavailable");
        await Assert.That(replay.FailureCode).IsEqualTo(sibling.FailureCode);

        var claimed = await _fixture.Repository.GetAsync(Tenant, task.Id);
        await Assert.That(claimed!.Status).IsEqualTo(UserTaskStatus.Assigned);
        await Assert.That(claimed.Assignee!.Provider).IsEqualTo("guest");
    }

    [Test]
    public async Task Verify_ReturnsTheSameFailureForUnknownExpiredAndWrongCodeInvitations()
    {
        var fixture = new UserTaskTestFixture(verifier: new UserTaskTestFixture.AcceptingVerifier());
        var manager = fixture.ManagerActor();
        var task = await fixture.ProjectAsync(fixture.Actor("user-1").Subject,
            definition => definition with { Invitations = [new("code", ["Approve"])] });

        await fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "code", ["Approve"]), manager);
        await fixture.DrainOutboxAsync();
        var token = fixture.Dispatcher.Token!;

        var unknown = await fixture.Invitations.VerifyAsync(new("not-a-real-token", "correct"));
        var wrongCode = await fixture.Invitations.VerifyAsync(new(token, "wrong"));
        await Assert.That(unknown.Succeeded).IsFalse();
        await Assert.That(wrongCode.Succeeded).IsFalse();
        await Assert.That(wrongCode.FailureCode).IsEqualTo(unknown.FailureCode);
        await Assert.That(unknown.TaskId).IsNull();
        await Assert.That(wrongCode.TaskId).IsNull();

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(30);
        var expired = await fixture.Invitations.VerifyAsync(new(token, "correct"));
        await Assert.That(expired.Succeeded).IsFalse();
        await Assert.That(expired.FailureCode).IsEqualTo(unknown.FailureCode);
    }

    [Test]
    public async Task Describe_ReturnsTheSameShapeForAnUnknownToken()
    {
        var known = await _fixture.Invitations.DescribeAsync("unknown-token-a");
        var other = await _fixture.Invitations.DescribeAsync("unknown-token-b");

        await Assert.That(other).IsEqualTo(known);
        await Assert.That(known.RequiresCode).IsTrue();
    }

    [Test]
    public async Task RateLimiter_StopsAcceptingOnceTheBudgetForACallerIsSpent()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new UserTasksOptions { AnonymousRateLimit = 3, AnonymousRateLimitWindow = TimeSpan.FromMinutes(5) });
        var limiter = new SlidingWindowUserTaskInvitationRateLimiter(_fixture.Clock, options);

        await Assert.That(await limiter.TryAcquireAsync("10.0.0.1")).IsTrue();
        await Assert.That(await limiter.TryAcquireAsync("10.0.0.1")).IsTrue();
        await Assert.That(await limiter.TryAcquireAsync("10.0.0.1")).IsTrue();
        await Assert.That(await limiter.TryAcquireAsync("10.0.0.1")).IsFalse();

        // A different caller has its own budget, and the window resets on its own.
        await Assert.That(await limiter.TryAcquireAsync("10.0.0.2")).IsTrue();
        _fixture.Clock.UtcNow = _fixture.Clock.UtcNow.AddMinutes(6);
        await Assert.That(await limiter.TryAcquireAsync("10.0.0.1")).IsTrue();
    }

    [Test]
    public async Task GuestSession_IsScopedToItsOwnTaskAndCannotReachAnother()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (guest, _) = await _fixture.IssueGuestSessionAsync(task, manager);

        var ownDetail = await Assert.That(await _fixture.Manager.GetAsync(Tenant, task.Id, guest)).IsNotNull();
        await Assert.That(ownDetail.Disclosure.GuestVisible).IsTrue();

        var other = await _fixture.Manager.ProjectAsync(new(Tenant, "definition-2", "instance-2", "activity-2", "bookmark-2",
            new() { Title = "Other", Actions = [new("Approve", "Approve")] }, [], [], _fixture.Clock.UtcNow, "task-2"));
        await Assert.That(await _fixture.Manager.GetAsync(Tenant, other.Task.Id, guest)).IsNull();

        var cross = await _fixture.Manager.CompleteAsync(Tenant, other.Task.Id, new(other.Task.Revision, "op-1", "Approve"), guest);
        await Assert.That(cross.Accepted).IsFalse();
        await Assert.That(cross.ConflictCode).IsEqualTo("forbidden");
    }

    [Test]
    public async Task GuestProjection_OmitsWorkflowContextParticipantsAndHistory()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (guest, _) = await _fixture.IssueGuestSessionAsync(task, manager);

        var detail = await Assert.That(await _fixture.Manager.GetAsync(Tenant, task.Id, guest)).IsNotNull();
        await Assert.That(detail.Workflow).IsNull();
        await Assert.That(detail.WorkflowInstanceId).IsNull();
        await Assert.That(detail.Assignee).IsNull();
        await Assert.That(detail.CandidateSummary).IsNull();
        await Assert.That(detail.Disclosure.CanViewHistory).IsFalse();

        var events = await Assert.That(await _fixture.Manager.GetEventsAsync(Tenant, task.Id, null, 50, guest)).IsNotNull();
        await Assert.That(events.Items).IsEmpty();
    }

    [Test]
    public async Task GuestCompletion_IsLimitedToTheActionsItsInvitationWasIssuedFor()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation("Approve"));
        var (guest, _) = await _fixture.IssueGuestSessionAsync(task, manager);
        var current = await _fixture.Repository.GetAsync(Tenant, task.Id);

        // "Reject" is a valid task action but was never granted to this guest link.
        var rejected = await _fixture.Manager.CompleteAsync(Tenant, task.Id, new(current!.Revision, "op-reject", "Reject"), guest);
        await Assert.That(rejected.Accepted).IsFalse();
        await Assert.That(rejected.ConflictCode).IsEqualTo("forbidden");

        var approved = await _fixture.Manager.CompleteAsync(Tenant, task.Id, new(current.Revision, "op-approve", "Approve"), guest);
        await Assert.That(approved.Accepted).IsTrue();
    }

    [Test]
    [Arguments(UserTaskAccessOperation.Claim)]
    [Arguments(UserTaskAccessOperation.Release)]
    [Arguments(UserTaskAccessOperation.Assign)]
    [Arguments(UserTaskAccessOperation.UpdateScheduling)]
    [Arguments(UserTaskAccessOperation.Cancel)]
    [Arguments(UserTaskAccessOperation.Manage)]
    [Arguments(UserTaskAccessOperation.IssueInvitation)]
    [Arguments(UserTaskAccessOperation.RetryResolution)]
    public async Task GuestSession_IsDeniedEveryManagementOperation(UserTaskAccessOperation operation)
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (guest, _) = await _fixture.IssueGuestSessionAsync(task, manager);
        var current = await _fixture.Repository.GetAsync(Tenant, task.Id);

        await Assert.That(await _fixture.Policy.AuthorizeAsync(current!, guest, operation)).IsFalse();
        await Assert.That(await _fixture.Policy.CreateScopeAsync(guest, UserTaskQueryScopeKind.Assigned)).IsNull();
    }

    [Test]
    public async Task RevokingAConsumedInvitationWithdrawsTheGuestSessionItIssued()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (_, credential) = await _fixture.IssueGuestSessionAsync(task, manager);

        var current = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;
        var invitation = await Assert.That(current.Invitations).HasSingleItem();
        // Verification consumed it; that is exactly the state a manager needs to be able to revoke.
        await Assert.That(invitation.Status).IsEqualTo(UserTaskInvitationStatus.Consumed);
        await Assert.That(await _fixture.GuestActors.ResolveAsync(credential)).IsNotNull();

        await Assert.That(await _fixture.Invitations.RevokeAsync(Tenant, task.Id, invitation.Id, current.Revision, manager)).IsTrue();

        // The credential must stop working immediately rather than living out its TTL.
        await Assert.That(await _fixture.GuestActors.ResolveAsync(credential)).IsNull();
        var revoked = await Assert.That((await _fixture.Repository.GetAsync(Tenant, task.Id))!.Invitations).HasSingleItem();
        await Assert.That(revoked.Status).IsEqualTo(UserTaskInvitationStatus.Revoked);
        await Assert.That(revoked.RevokedAt).IsNotNull();

        // Asserted at the credential, which is the actual boundary: a guest actor exists only because the
        // resolver produced one from a live session, so once the credential is dead no guest principal can
        // be formed and the request is rejected before it reaches the manager.
        await Assert.That(await _fixture.GuestActors.ResolveAsync(credential)).IsNull();
    }

    [Test]
    public async Task RevokingOneInvitationLeavesOtherGuestSessionsOnTheSameTaskIntact()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject,
            definition => definition with
            {
                Invitations =
                [
                    new("bearer-a", ["Approve"], BearerOnly: true),
                    new("bearer-b", ["Approve"], BearerOnly: true)
                ]
            });

        // A is verified, so it holds a live session. B is issued but never verified.
        var (_, credentialA) = await _fixture.IssueGuestSessionAsync(task, manager, "bearer-a");
        var afterA = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;
        await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(afterA.Revision, "bearer-b", ["Approve"]), manager);

        var beforeRevoke = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;
        var invitationB = beforeRevoke.Invitations.Single(x => x.VerifierName == "bearer-b");
        await Assert.That(await _fixture.Invitations.RevokeAsync(Tenant, task.Id, invitationB.Id, beforeRevoke.Revision, manager)).IsTrue();

        // Revocation is scoped to the invitation, so A's session survives B being withdrawn.
        await Assert.That(await _fixture.GuestActors.ResolveAsync(credentialA)).IsNotNull();
        var after = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;
        await Assert.That(after.Invitations.Single(x => x.VerifierName == "bearer-b").Status).IsEqualTo(UserTaskInvitationStatus.Revoked);
        await Assert.That(after.Invitations.Single(x => x.VerifierName == "bearer-a").Status).IsEqualTo(UserTaskInvitationStatus.Consumed);
    }

    [Test]
    public async Task ARevocationThatFailsInTheSessionStoreLeavesTheInvitationRetryable()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (_, credential) = await _fixture.IssueGuestSessionAsync(task, manager);

        // One injected failure in the session store, then it recovers.
        var faulty = new UserTaskTestFixture.FaultyRevocationSessionIssuer(_fixture.GuestSessions, failures: 1);
        var invitations = new DefaultUserTaskInvitationService(_fixture.Repository, _fixture.Policy, _fixture.Outbox,
            new DefaultUserTaskInvitationVerifier(), faulty, _fixture.Sink, _fixture.Identity, _fixture.Clock, _fixture.Options);

        var before = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;
        var invitation = await Assert.That(before.Invitations).HasSingleItem();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => invitations.RevokeAsync(Tenant, task.Id, invitation.Id, before.Revision, manager));

        // The failure must not commit the terminal state, or the retry guard would reject the repair and
        // strand a live credential.
        var afterFailure = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;
        var invitationAfterFailure = await Assert.That(afterFailure.Invitations).HasSingleItem();
        await Assert.That(invitationAfterFailure.Status).IsNotEqualTo(UserTaskInvitationStatus.Revoked);

        await Assert.That(await invitations.RevokeAsync(Tenant, task.Id, invitation.Id, afterFailure.Revision, manager)).IsTrue();
        await Assert.That(await _fixture.GuestActors.ResolveAsync(credential)).IsNull();
        var revoked = await Assert.That((await _fixture.Repository.GetAsync(Tenant, task.Id))!.Invitations).HasSingleItem();
        await Assert.That(revoked.Status).IsEqualTo(UserTaskInvitationStatus.Revoked);
    }

    [Test]
    public async Task RetryingRevocationOnAnAlreadyRevokedInvitationStillSweepsItsSessions()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (_, credential) = await _fixture.IssueGuestSessionAsync(task, manager);
        var before = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;
        var invitation = await Assert.That(before.Invitations).HasSingleItem();

        await Assert.That(await _fixture.Invitations.RevokeAsync(Tenant, task.Id, invitation.Id, before.Revision, manager)).IsTrue();

        // A second call is idempotently successful and re-runs the sweep, so a caller repairing a partial
        // failure is never told "no" on an invitation whose sessions might still be live.
        var after = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;
        await Assert.That(await _fixture.Invitations.RevokeAsync(Tenant, task.Id, invitation.Id, after.Revision, manager)).IsTrue();
        await Assert.That(await _fixture.GuestActors.ResolveAsync(credential)).IsNull();
    }

    [Test]
    public async Task AnInvitationRevokedWhileVerificationIsInFlightDoesNotYieldALiveCredential()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Approve"]), manager);
        await _fixture.DrainOutboxAsync();
        var token = _fixture.Dispatcher.Token!;

        // Revoke the moment the session lands in the store, which is the window where the manager's sweep
        // finds nothing and verification would otherwise hand back a credential that outlives the revoke.
        var racing = new RevokeOnIssueSessionIssuer(_fixture.GuestSessions, async () =>
        {
            var current = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;
            var invitation = await Assert.That(current.Invitations).HasSingleItem();
            await _fixture.Invitations.RevokeAsync(Tenant, task.Id, invitation.Id, current.Revision, manager);
        });
        var invitations = new DefaultUserTaskInvitationService(_fixture.Repository, _fixture.Policy, _fixture.Outbox,
            new DefaultUserTaskInvitationVerifier(), racing, _fixture.Sink, _fixture.Identity, _fixture.Clock, _fixture.Options);

        var verified = await invitations.VerifyAsync(new(token));

        await Assert.That(verified.Succeeded).IsFalse();
        await Assert.That(verified.FailureCode).IsEqualTo("invitation-unavailable");
        await Assert.That(verified.SessionToken).IsNull();
    }

    /// <summary>Runs a callback immediately after a session is issued, to drive the revoke-during-verify race.</summary>
    private sealed class RevokeOnIssueSessionIssuer(IUserTaskGuestSessionIssuer inner, Func<Task> afterIssue) : IUserTaskGuestSessionIssuer
    {
        public async Task<GuestSessionResult> IssueAsync(UserTaskInvitation invitation, ParticipantReference subject, CancellationToken cancellationToken = default)
        {
            var result = await inner.IssueAsync(invitation, subject, cancellationToken);
            await afterIssue();
            return result;
        }

        public Task<UserTaskGuestSession?> ResolveAsync(string credential, CancellationToken cancellationToken = default) => inner.ResolveAsync(credential, cancellationToken);
        public Task RevokeForTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken = default) => inner.RevokeForTaskAsync(tenantId, taskId, cancellationToken);
        public Task RevokeForInvitationAsync(string tenantId, string invitationId, CancellationToken cancellationToken = default) => inner.RevokeForInvitationAsync(tenantId, invitationId, cancellationToken);
    }

    [Test]
    public async Task ASuccessfulRevocationSweepsSessionsOnBothSidesOfTheCommit()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (_, credential) = await _fixture.IssueGuestSessionAsync(task, manager);

        var counting = new UserTaskTestFixture.FaultyRevocationSessionIssuer(_fixture.GuestSessions, failures: 0);
        var invitations = new DefaultUserTaskInvitationService(_fixture.Repository, _fixture.Policy, _fixture.Outbox,
            new DefaultUserTaskInvitationVerifier(), counting, _fixture.Sink, _fixture.Identity, _fixture.Clock, _fixture.Options);

        var before = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;
        var invitation = await Assert.That(before.Invitations).HasSingleItem();
        await Assert.That(await invitations.RevokeAsync(Tenant, task.Id, invitation.Id, before.Revision, manager)).IsTrue();

        // Both sweeps are load-bearing: the first keeps a store failure from committing, the second catches
        // a session a concurrent verification issued between the first sweep and the commit.
        await Assert.That(counting.RevokeCallCount).IsEqualTo(2);
        await Assert.That(await _fixture.GuestActors.ResolveAsync(credential)).IsNull();
    }

    [Test]
    public async Task RevokingAnUnknownInvitationIsRefused()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Approve"]), manager);
        var current = (await _fixture.Repository.GetAsync(Tenant, task.Id))!;

        // Retrying a revoked invitation is deliberately idempotent so a partial failure stays repairable;
        // an invitation that does not exist is still a plain refusal.
        await Assert.That(await _fixture.Invitations.RevokeAsync(Tenant, task.Id, "no-such-invitation", current.Revision, manager)).IsFalse();
    }

    [Test]
    public async Task GuestSession_StopsResolvingOnceTheTaskCloses()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (_, credential) = await _fixture.IssueGuestSessionAsync(task, manager);
        await Assert.That(await _fixture.GuestActors.ResolveAsync(credential)).IsNotNull();

        await _fixture.Projection.FinalizeBookmarkRemovalAsync(new(Tenant, task.Id, task.BookmarkId, _fixture.Clock.UtcNow));

        await Assert.That(await _fixture.GuestActors.ResolveAsync(credential)).IsNull();
    }

    [Test]
    public async Task GuestSession_ExpiresAtTheHostCeilingEvenWhenTheInvitationLivesLonger()
    {
        var options = new UserTasksOptions { GuestSessionLifetime = TimeSpan.FromMinutes(30), DefaultInvitationLifetime = TimeSpan.FromDays(7) };
        var fixture = new UserTaskTestFixture(options);
        var manager = fixture.ManagerActor();
        var task = await fixture.ProjectAsync(fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (_, credential) = await fixture.IssueGuestSessionAsync(task, manager);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddMinutes(31);

        await Assert.That(await fixture.GuestActors.ResolveAsync(credential)).IsNull();
    }

    [Test]
    public async Task Outbox_RetriesADispatchFailureAndAbandonsItOnceTheScheduleIsExhausted()
    {
        var options = new UserTasksOptions { InvitationDeliveryRetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)] };
        var fixture = new UserTaskTestFixture(options);
        var manager = fixture.ManagerActor();
        var task = await fixture.ProjectAsync(fixture.Actor("user-1").Subject, WithBearerInvitation());
        await fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Approve"]), manager);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var due = await Assert.That(await fixture.Outbox.DequeueDueAsync(10)).HasSingleItem();
            await fixture.Outbox.RescheduleAsync(due.Id, fixture.Clock.UtcNow);
        }

        // The schedule is exhausted, so the encrypted secret is dropped rather than retried forever.
        var third = await Assert.That(await fixture.Outbox.DequeueDueAsync(10)).HasSingleItem();
        await fixture.Outbox.RescheduleAsync(third.Id, fixture.Clock.UtcNow);
        await Assert.That(await fixture.Outbox.DequeueDueAsync(10)).IsEmpty();
    }

    [Test]
    public async Task GuestActorResolver_ReadsOnlyItsOwnAuthorizationScheme()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (_, credential) = await _fixture.IssueGuestSessionAsync(task, manager);

        await Assert.That(UserTaskGuestActorResolver.ReadCredential($"UserTaskSession {credential}")).IsEqualTo(credential);
        await Assert.That(UserTaskGuestActorResolver.ReadCredential($"Bearer {credential}")).IsNull();
        await Assert.That(UserTaskGuestActorResolver.ReadCredential(null)).IsNull();
        await Assert.That(await _fixture.GuestActors.ResolveAsync("not-a-session")).IsNull();
    }
}
