using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Elsa.UserTasks.Persistence.ConformanceTests.Providers;

namespace Elsa.UserTasks.Persistence.ConformanceTests;

/// <summary>
/// The behaviour every <see cref="IUserTaskGuestSessionIssuer"/> owes its callers. A guest credential is a
/// bearer secret with no identity behind it, so "revoked" has to mean revoked in every store, immediately.
/// </summary>
public abstract class UserTaskGuestSessionConformanceTests(UserTaskStoreFixture fixture) : UserTaskConformanceTestBase(fixture)
{
    private IUserTaskGuestSessionIssuer Sessions => Fixture.GuestSessions;

    [ConformanceFact]
    public async Task AnIssuedCredentialResolvesToItsInvitationsSubjectAndActions()
    {
        await ActivateAsync();
        var subject = Subject("guest-1");
        var issued = await IssueAsync(Invitation(), subject);

        var session = await Sessions.ResolveAsync(issued.Token!);

        Assert.True(issued.Succeeded);
        Assert.NotNull(session);
        Assert.Equal(TenantId, session!.TenantId);
        Assert.Equal("Complete", Assert.Single(session.AllowedActions));
        Assert.True(subject.Matches(session.Subject));
    }

    [ConformanceFact]
    public async Task AnUnknownOrEmptyCredentialResolvesToNothing()
    {
        await ActivateAsync();
        await IssueAsync(Invitation(), Subject("guest-1"));

        Assert.Null(await Sessions.ResolveAsync($"not-a-credential-{Guid.NewGuid():N}"));
        Assert.Null(await Sessions.ResolveAsync(""));
        Assert.Null(await Sessions.ResolveAsync("   "));
    }

    [ConformanceFact]
    public async Task RevokingForATaskKillsEveryCredentialIssuedForIt()
    {
        await ActivateAsync();
        var taskId = $"task-{Guid.NewGuid():N}";
        var first = await IssueAsync(Invitation(taskId: taskId, id: "invitation-a"), Subject("guest-1"));
        var second = await IssueAsync(Invitation(taskId: taskId, id: "invitation-b"), Subject("guest-2"));

        await Sessions.RevokeForTaskAsync(TenantId, taskId);

        Assert.Null(await Sessions.ResolveAsync(first.Token!));
        Assert.Null(await Sessions.ResolveAsync(second.Token!));
    }

    [ConformanceFact]
    public async Task RevokingForOneInvitationLeavesAnotherInvitationsSessionAlive()
    {
        await ActivateAsync();
        var taskId = $"task-{Guid.NewGuid():N}";
        var revoked = await IssueAsync(Invitation(taskId: taskId, id: "invitation-a"), Subject("guest-1"));
        var survivor = await IssueAsync(Invitation(taskId: taskId, id: "invitation-b"), Subject("guest-2"));

        await Sessions.RevokeForInvitationAsync(TenantId, "invitation-a");

        // Scoped revocation is the whole point: withdrawing one guest link must not sign the other guest
        // out, and must not leave the withdrawn one usable either.
        Assert.Null(await Sessions.ResolveAsync(revoked.Token!));
        Assert.NotNull(await Sessions.ResolveAsync(survivor.Token!));
    }

    [ConformanceFact]
    public async Task RevocationIsScopedByTenant()
    {
        await ActivateAsync();
        var issued = await IssueAsync(Invitation(id: "invitation-a"), Subject("guest-1"));

        await Sessions.RevokeForInvitationAsync("other-tenant", "invitation-a");
        Assert.NotNull(await Sessions.ResolveAsync(issued.Token!));

        await Sessions.RevokeForTaskAsync("other-tenant", "task-1");
        Assert.NotNull(await Sessions.ResolveAsync(issued.Token!));
    }

    [ConformanceFact]
    public async Task RevokingTwiceIsHarmless()
    {
        await ActivateAsync();
        var issued = await IssueAsync(Invitation(id: "invitation-a"), Subject("guest-1"));

        await Sessions.RevokeForInvitationAsync(TenantId, "invitation-a");
        await Sessions.RevokeForInvitationAsync(TenantId, "invitation-a");

        Assert.Null(await Sessions.ResolveAsync(issued.Token!));
    }

    [ConformanceFact]
    public async Task ASessionStopsResolvingOnceItExpiresWithoutAnExplicitRevoke()
    {
        await ActivateAsync();
        Fixture.Settings.GuestSessionLifetime = TimeSpan.FromMinutes(30);
        var issued = await IssueAsync(Invitation(expiresAt: Clock.UtcNow.AddDays(1)), Subject("guest-1"));
        Assert.NotNull(await Sessions.ResolveAsync(issued.Token!));

        Clock.Advance(TimeSpan.FromMinutes(31));

        Assert.Null(await Sessions.ResolveAsync(issued.Token!));
    }

    [ConformanceFact]
    public async Task ASessionNeverOutlivesTheInvitationItCameFrom()
    {
        await ActivateAsync();
        Fixture.Settings.GuestSessionLifetime = TimeSpan.FromDays(7);
        var invitationExpiry = Clock.UtcNow.AddMinutes(10);

        var issued = await IssueAsync(Invitation(expiresAt: invitationExpiry), Subject("guest-1"));

        Assert.Equal(invitationExpiry, issued.ExpiresAt);
        Clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Null(await Sessions.ResolveAsync(issued.Token!));
    }

    [ConformanceFact]
    public async Task AnAlreadyExpiredInvitationIssuesNothingAtAll()
    {
        await ActivateAsync();

        var issued = await IssueAsync(Invitation(expiresAt: Clock.UtcNow.AddMinutes(-1)), Subject("guest-1"));

        Assert.False(issued.Succeeded);
        Assert.Null(issued.Token);
        Assert.Equal("session-unavailable", issued.FailureCode);
    }

    [ConformanceFact]
    public async Task TheRawCredentialIsNeverRecoverableFromTheStore()
    {
        await ActivateAsync();
        var issued = await IssueAsync(Invitation(), Subject("guest-1"));
        var session = await Sessions.ResolveAsync(issued.Token!);

        // The credential is a bearer secret: the store keeps a hash, so nothing it exposes can be replayed.
        Assert.NotNull(session);
        Assert.DoesNotContain(issued.Token!, System.Text.Json.JsonSerializer.Serialize(session), StringComparison.Ordinal);
    }

    private Task<GuestSessionResult> IssueAsync(UserTaskInvitation invitation, ParticipantReference subject) =>
        Sessions.IssueAsync(invitation, subject);

    private UserTaskInvitation Invitation(string? taskId = null, string id = "invitation-1", DateTimeOffset? expiresAt = null) =>
        new(id, TenantId, taskId ?? $"task-{Guid.NewGuid():N}", "guest@example.com", $"HASH-{Guid.NewGuid():N}",
            UserTaskInvitationStatus.Consumed, Clock.UtcNow, expiresAt ?? Clock.UtcNow.AddDays(1), "bearer")
        {
            AllowedActions = ["Complete"]
        };
}
