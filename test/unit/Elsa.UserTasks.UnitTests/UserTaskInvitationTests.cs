using System.Text.Json;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Services;
using Xunit;

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
        definition => definition with { Invitations = [new("bearer", actions.Length > 0 ? actions : ["Approve"], BearerOnly: true)] };

    [Fact]
    public async Task Issue_KeepsTheSecretOutOfTheApiResultAndOffTheAuditTrail()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());

        var issued = await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Approve"]), manager);
        Assert.NotNull(issued);

        await _fixture.DrainOutboxAsync();
        var token = _fixture.Dispatcher.Token;
        Assert.NotNull(token);
        Assert.DoesNotContain(token, JsonSerializer.Serialize(issued));

        var stored = await _fixture.Repository.GetAsync(Tenant, task.Id);
        Assert.DoesNotContain(token, JsonSerializer.Serialize(stored!.Events));
        Assert.DoesNotContain(token, JsonSerializer.Serialize(stored.Invitations));
    }

    [Fact]
    public async Task Issue_RefusesToBroadenAnInvitationBeyondTheActivityDefinition()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation("Approve"));

        // "Reject" is a configured task action but was not part of the materialized invitation definition.
        Assert.Null(await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Approve", "Reject"]), manager));
        Assert.Null(await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Reject"]), manager));
    }

    [Fact]
    public async Task Issue_RequiresTheInvitePermissionAndManagerRelationship()
    {
        var participant = _fixture.Actor("user-1", "read:user-tasks", "invite:user-tasks");
        var task = await _fixture.ProjectAsync(participant.Subject, WithBearerInvitation());

        Assert.Null(await _fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Approve"]), participant));
    }

    [Fact]
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
        Assert.True(verified.Succeeded);
        Assert.Equal(task.Id, verified.TaskId);
        Assert.NotNull(verified.SessionToken);

        // The sibling and a replay of the winner are both rejected, with the same opaque code.
        var sibling = await _fixture.Invitations.VerifyAsync(new(second));
        var replay = await _fixture.Invitations.VerifyAsync(new(first));
        Assert.False(sibling.Succeeded);
        Assert.False(replay.Succeeded);
        Assert.Equal("invitation-unavailable", sibling.FailureCode);
        Assert.Equal(sibling.FailureCode, replay.FailureCode);

        var claimed = await _fixture.Repository.GetAsync(Tenant, task.Id);
        Assert.Equal(UserTaskStatus.Assigned, claimed!.Status);
        Assert.Equal("guest", claimed.Assignee!.Provider);
    }

    [Fact]
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
        Assert.False(unknown.Succeeded);
        Assert.False(wrongCode.Succeeded);
        Assert.Equal(unknown.FailureCode, wrongCode.FailureCode);
        Assert.Null(unknown.TaskId);
        Assert.Null(wrongCode.TaskId);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(30);
        var expired = await fixture.Invitations.VerifyAsync(new(token, "correct"));
        Assert.False(expired.Succeeded);
        Assert.Equal(unknown.FailureCode, expired.FailureCode);
    }

    [Fact]
    public async Task Describe_ReturnsTheSameShapeForAnUnknownToken()
    {
        var known = await _fixture.Invitations.DescribeAsync("unknown-token-a");
        var other = await _fixture.Invitations.DescribeAsync("unknown-token-b");

        Assert.Equal(known, other);
        Assert.True(known.RequiresCode);
    }

    [Fact]
    public async Task RateLimiter_StopsAcceptingOnceTheBudgetForACallerIsSpent()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new UserTasksOptions { AnonymousRateLimit = 3, AnonymousRateLimitWindow = TimeSpan.FromMinutes(5) });
        var limiter = new SlidingWindowUserTaskInvitationRateLimiter(_fixture.Clock, options);

        Assert.True(await limiter.TryAcquireAsync("10.0.0.1"));
        Assert.True(await limiter.TryAcquireAsync("10.0.0.1"));
        Assert.True(await limiter.TryAcquireAsync("10.0.0.1"));
        Assert.False(await limiter.TryAcquireAsync("10.0.0.1"));

        // A different caller has its own budget, and the window resets on its own.
        Assert.True(await limiter.TryAcquireAsync("10.0.0.2"));
        _fixture.Clock.UtcNow = _fixture.Clock.UtcNow.AddMinutes(6);
        Assert.True(await limiter.TryAcquireAsync("10.0.0.1"));
    }

    [Fact]
    public async Task GuestSession_IsScopedToItsOwnTaskAndCannotReachAnother()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (guest, _) = await _fixture.IssueGuestSessionAsync(task, manager);

        var ownDetail = await _fixture.Manager.GetAsync(Tenant, task.Id, guest);
        Assert.NotNull(ownDetail);
        Assert.True(ownDetail.Disclosure.GuestVisible);

        var other = await _fixture.Manager.ProjectAsync(new(Tenant, "definition-2", "instance-2", "activity-2", "bookmark-2",
            new() { Title = "Other", Actions = [new("Approve", "Approve")] }, [], [], _fixture.Clock.UtcNow, "task-2"));
        Assert.Null(await _fixture.Manager.GetAsync(Tenant, other.Task.Id, guest));

        var cross = await _fixture.Manager.CompleteAsync(Tenant, other.Task.Id, new(other.Task.Revision, "op-1", "Approve"), guest);
        Assert.False(cross.Accepted);
        Assert.Equal("forbidden", cross.ConflictCode);
    }

    [Fact]
    public async Task GuestProjection_OmitsWorkflowContextParticipantsAndHistory()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (guest, _) = await _fixture.IssueGuestSessionAsync(task, manager);

        var detail = await _fixture.Manager.GetAsync(Tenant, task.Id, guest);
        Assert.NotNull(detail);
        Assert.Null(detail.Workflow);
        Assert.Null(detail.WorkflowInstanceId);
        Assert.Null(detail.Assignee);
        Assert.Null(detail.CandidateSummary);
        Assert.False(detail.Disclosure.CanViewHistory);

        var events = await _fixture.Manager.GetEventsAsync(Tenant, task.Id, null, 50, guest);
        Assert.NotNull(events);
        Assert.Empty(events.Items);
    }

    [Fact]
    public async Task GuestCompletion_IsLimitedToTheActionsItsInvitationWasIssuedFor()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation("Approve"));
        var (guest, _) = await _fixture.IssueGuestSessionAsync(task, manager);
        var current = await _fixture.Repository.GetAsync(Tenant, task.Id);

        // "Reject" is a valid task action but was never granted to this guest link.
        var rejected = await _fixture.Manager.CompleteAsync(Tenant, task.Id, new(current!.Revision, "op-reject", "Reject"), guest);
        Assert.False(rejected.Accepted);
        Assert.Equal("forbidden", rejected.ConflictCode);

        var approved = await _fixture.Manager.CompleteAsync(Tenant, task.Id, new(current.Revision, "op-approve", "Approve"), guest);
        Assert.True(approved.Accepted);
    }

    [Theory]
    [InlineData(UserTaskAccessOperation.Claim)]
    [InlineData(UserTaskAccessOperation.Release)]
    [InlineData(UserTaskAccessOperation.Assign)]
    [InlineData(UserTaskAccessOperation.UpdateScheduling)]
    [InlineData(UserTaskAccessOperation.Cancel)]
    [InlineData(UserTaskAccessOperation.Manage)]
    [InlineData(UserTaskAccessOperation.IssueInvitation)]
    [InlineData(UserTaskAccessOperation.RetryResolution)]
    public async Task GuestSession_IsDeniedEveryManagementOperation(UserTaskAccessOperation operation)
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (guest, _) = await _fixture.IssueGuestSessionAsync(task, manager);
        var current = await _fixture.Repository.GetAsync(Tenant, task.Id);

        Assert.False(await _fixture.Policy.AuthorizeAsync(current!, guest, operation));
        Assert.Null(await _fixture.Policy.CreateScopeAsync(guest, UserTaskQueryScopeKind.Assigned));
    }

    [Fact]
    public async Task GuestSession_StopsResolvingOnceTheTaskCloses()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (_, credential) = await _fixture.IssueGuestSessionAsync(task, manager);
        Assert.NotNull(await _fixture.GuestActors.ResolveAsync(credential));

        await _fixture.Projection.FinalizeBookmarkRemovalAsync(new(Tenant, task.Id, task.BookmarkId, _fixture.Clock.UtcNow));

        Assert.Null(await _fixture.GuestActors.ResolveAsync(credential));
    }

    [Fact]
    public async Task GuestSession_ExpiresAtTheHostCeilingEvenWhenTheInvitationLivesLonger()
    {
        var options = new UserTasksOptions { GuestSessionLifetime = TimeSpan.FromMinutes(30), DefaultInvitationLifetime = TimeSpan.FromDays(7) };
        var fixture = new UserTaskTestFixture(options);
        var manager = fixture.ManagerActor();
        var task = await fixture.ProjectAsync(fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (_, credential) = await fixture.IssueGuestSessionAsync(task, manager);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddMinutes(31);

        Assert.Null(await fixture.GuestActors.ResolveAsync(credential));
    }

    [Fact]
    public async Task Outbox_RetriesADispatchFailureAndAbandonsItOnceTheScheduleIsExhausted()
    {
        var options = new UserTasksOptions { InvitationDeliveryRetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)] };
        var fixture = new UserTaskTestFixture(options);
        var manager = fixture.ManagerActor();
        var task = await fixture.ProjectAsync(fixture.Actor("user-1").Subject, WithBearerInvitation());
        await fixture.Invitations.IssueAsync(Tenant, task.Id, new(task.Revision, "bearer", ["Approve"]), manager);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var due = Assert.Single(await fixture.Outbox.DequeueDueAsync(10));
            await fixture.Outbox.RescheduleAsync(due.Id, fixture.Clock.UtcNow);
        }

        // The schedule is exhausted, so the encrypted secret is dropped rather than retried forever.
        var third = Assert.Single(await fixture.Outbox.DequeueDueAsync(10));
        await fixture.Outbox.RescheduleAsync(third.Id, fixture.Clock.UtcNow);
        Assert.Empty(await fixture.Outbox.DequeueDueAsync(10));
    }

    [Fact]
    public async Task GuestActorResolver_ReadsOnlyItsOwnAuthorizationScheme()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, WithBearerInvitation());
        var (_, credential) = await _fixture.IssueGuestSessionAsync(task, manager);

        Assert.Equal(credential, UserTaskGuestActorResolver.ReadCredential($"UserTaskSession {credential}"));
        Assert.Null(UserTaskGuestActorResolver.ReadCredential($"Bearer {credential}"));
        Assert.Null(UserTaskGuestActorResolver.ReadCredential(null));
        Assert.Null(await _fixture.GuestActors.ResolveAsync("not-a-session"));
    }
}
