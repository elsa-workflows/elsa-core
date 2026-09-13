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

    [Test]
    public async Task AnIssuedCredentialResolvesToItsInvitationsSubjectAndActions()
    {
        await ActivateAsync();
        var subject = Subject("guest-1");
        var issued = await IssueAsync(Invitation(), subject);

        var session = await Sessions.ResolveAsync(issued.Token!);

        await Assert.That(issued.Succeeded).IsTrue();
        var resolvedSession = await Assert.That(session).IsNotNull();
        await Assert.That(resolvedSession.TenantId).IsEqualTo(TenantId);
        var allowedAction = await Assert.That(resolvedSession.AllowedActions).HasSingleItem();
        await Assert.That(allowedAction).IsEqualTo("Complete");
        await Assert.That(subject.Matches(resolvedSession.Subject)).IsTrue();
    }

    [Test]
    public async Task AnUnknownOrEmptyCredentialResolvesToNothing()
    {
        await ActivateAsync();
        await IssueAsync(Invitation(), Subject("guest-1"));

        await Assert.That(await Sessions.ResolveAsync($"not-a-credential-{Guid.NewGuid():N}")).IsNull();
        await Assert.That(await Sessions.ResolveAsync("")).IsNull();
        await Assert.That(await Sessions.ResolveAsync("   ")).IsNull();
    }

    [Test]
    public async Task RevokingForATaskKillsEveryCredentialIssuedForIt()
    {
        await ActivateAsync();
        var taskId = $"task-{Guid.NewGuid():N}";
        var first = await IssueAsync(Invitation(taskId: taskId, id: "invitation-a"), Subject("guest-1"));
        var second = await IssueAsync(Invitation(taskId: taskId, id: "invitation-b"), Subject("guest-2"));

        await Sessions.RevokeForTaskAsync(TenantId, taskId);

        await Assert.That(await Sessions.ResolveAsync(first.Token!)).IsNull();
        await Assert.That(await Sessions.ResolveAsync(second.Token!)).IsNull();
    }

    [Test]
    public async Task RevokingForOneInvitationLeavesAnotherInvitationsSessionAlive()
    {
        await ActivateAsync();
        var taskId = $"task-{Guid.NewGuid():N}";
        var revoked = await IssueAsync(Invitation(taskId: taskId, id: "invitation-a"), Subject("guest-1"));
        var survivor = await IssueAsync(Invitation(taskId: taskId, id: "invitation-b"), Subject("guest-2"));

        await Sessions.RevokeForInvitationAsync(TenantId, "invitation-a");

        // Scoped revocation is the whole point: withdrawing one guest link must not sign the other guest
        // out, and must not leave the withdrawn one usable either.
        await Assert.That(await Sessions.ResolveAsync(revoked.Token!)).IsNull();
        await Assert.That(await Sessions.ResolveAsync(survivor.Token!)).IsNotNull();
    }

    [Test]
    public async Task RevocationIsScopedByTenant()
    {
        await ActivateAsync();
        var issued = await IssueAsync(Invitation(id: "invitation-a"), Subject("guest-1"));

        await Sessions.RevokeForInvitationAsync("other-tenant", "invitation-a");
        await Assert.That(await Sessions.ResolveAsync(issued.Token!)).IsNotNull();

        await Sessions.RevokeForTaskAsync("other-tenant", "task-1");
        await Assert.That(await Sessions.ResolveAsync(issued.Token!)).IsNotNull();
    }

    [Test]
    public async Task RevokingTwiceIsHarmless()
    {
        await ActivateAsync();
        var issued = await IssueAsync(Invitation(id: "invitation-a"), Subject("guest-1"));

        await Sessions.RevokeForInvitationAsync(TenantId, "invitation-a");
        await Sessions.RevokeForInvitationAsync(TenantId, "invitation-a");

        await Assert.That(await Sessions.ResolveAsync(issued.Token!)).IsNull();
    }

    [Test]
    public async Task ASessionStopsResolvingOnceItExpiresWithoutAnExplicitRevoke()
    {
        await ActivateAsync();
        Fixture.Settings.GuestSessionLifetime = TimeSpan.FromMinutes(30);
        var issued = await IssueAsync(Invitation(expiresAt: Clock.UtcNow.AddDays(1)), Subject("guest-1"));
        await Assert.That(await Sessions.ResolveAsync(issued.Token!)).IsNotNull();

        Clock.Advance(TimeSpan.FromMinutes(31));

        await Assert.That(await Sessions.ResolveAsync(issued.Token!)).IsNull();
    }

    [Test]
    public async Task ASessionNeverOutlivesTheInvitationItCameFrom()
    {
        await ActivateAsync();
        Fixture.Settings.GuestSessionLifetime = TimeSpan.FromDays(7);
        var invitationExpiry = Clock.UtcNow.AddMinutes(10);

        var issued = await IssueAsync(Invitation(expiresAt: invitationExpiry), Subject("guest-1"));

        await Assert.That(issued.ExpiresAt).IsEqualTo(invitationExpiry);
        Clock.Advance(TimeSpan.FromMinutes(11));
        await Assert.That(await Sessions.ResolveAsync(issued.Token!)).IsNull();
    }

    [Test]
    public async Task AnAlreadyExpiredInvitationIssuesNothingAtAll()
    {
        await ActivateAsync();

        var issued = await IssueAsync(Invitation(expiresAt: Clock.UtcNow.AddMinutes(-1)), Subject("guest-1"));

        await Assert.That(issued.Succeeded).IsFalse();
        await Assert.That(issued.Token).IsNull();
        await Assert.That(issued.FailureCode).IsEqualTo("session-unavailable");
    }

    [Test]
    public async Task TheRawCredentialIsNeverRecoverableFromTheStore()
    {
        await ActivateAsync();
        var issued = await IssueAsync(Invitation(), Subject("guest-1"));
        var session = await Sessions.ResolveAsync(issued.Token!);

        // The credential is a bearer secret: the store keeps a hash, so nothing it exposes can be replayed.
        var resolvedSession = await Assert.That(session).IsNotNull();
        await Assert.That(System.Text.Json.JsonSerializer.Serialize(resolvedSession)).DoesNotContain(issued.Token!, StringComparison.Ordinal);
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
