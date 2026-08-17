using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Elsa.Common;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.Workflows;
using Microsoft.Extensions.Options;

namespace Elsa.UserTasks.Services;

/// <summary>
/// Implements the invitation protocol for the in-memory Core provider. A host that needs invitations
/// to survive a restart should replace this service with a provider-backed implementation; the public
/// contract deliberately keeps token material out of UserTask and notification models.
/// </summary>
public sealed class DefaultUserTaskInvitationService(
    IUserTaskRepository repository,
    IUserTaskAccessPolicy accessPolicy,
    IUserTaskInvitationDispatcher dispatcher,
    IUserTaskInvitationVerifier verifier,
    IUserTaskGuestSessionIssuer sessionIssuer,
    IUserTaskNotificationSink notifications,
    IIdentityGenerator identityGenerator,
    ISystemClock clock,
    IOptions<UserTasksOptions> options) : IUserTaskInvitationService
{
    private readonly ConcurrentDictionary<string, TokenEntry> _tokens = new(StringComparer.Ordinal);

    public async Task<UserTaskInvitationIssueResult?> IssueAsync(string tenantId, string taskId, UserTaskInvitationIssueRequest request, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null || !await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.IssueInvitation, cancellationToken))
            return null;
        if (task.IsTerminal || request.ExpectedRevision != task.Revision || string.IsNullOrWhiteSpace(request.VerifierName))
            return null;

        var actions = request.AllowedActions.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        // Match the requested action set exactly against one of the activity's invitation definitions.
        // A manager cannot broaden a guest link beyond what the workflow designer materialized.
        var definition = task.InvitationDefinitions.FirstOrDefault(x =>
            string.Equals(x.VerifierName, request.VerifierName, StringComparison.OrdinalIgnoreCase)
            && x.AllowedActions.Count == actions.Length
            && x.AllowedActions.All(allowed => actions.Contains(allowed, StringComparer.OrdinalIgnoreCase)));
        if (definition == null || actions.Length == 0)
            return null;

        var now = clock.UtcNow;
        var lifetime = request.Lifetime ?? definition.Lifetime ?? options.Value.DefaultInvitationLifetime;
        if (lifetime <= TimeSpan.Zero)
            return null;
        var expiresAt = now.Add(lifetime);
        if (task.DueAt is { } dueAt && dueAt < expiresAt)
            expiresAt = dueAt;
        if (expiresAt <= now)
            return null;

        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(token);
        var siblingGroupId = task.Invitations.FirstOrDefault(x =>
            string.Equals(x.VerifierName, request.VerifierName, StringComparison.OrdinalIgnoreCase)
            && x.Status is (UserTaskInvitationStatus.Pending or UserTaskInvitationStatus.Dispatched))?.SiblingGroupId
            ?? identityGenerator.GenerateId();
        var invitation = new UserTaskInvitation(
            identityGenerator.GenerateId(), tenantId, taskId, request.Recipient, tokenHash,
            UserTaskInvitationStatus.Pending, now, expiresAt, request.VerifierName,
            SiblingGroupId: siblingGroupId);

        task.Invitations.Add(invitation);
        task.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), tenantId, taskId, task.Revision + 1,
            "InvitationIssued", now, actor.Subject, request.OperationId));
        try
        {
            await repository.SaveAsync(task, request.ExpectedRevision, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        _tokens[tokenHash] = new TokenEntry(tenantId, taskId, invitation.Id);
        var committed = await repository.GetAsync(tenantId, taskId, cancellationToken) ?? task;
        await notifications.PublishAsync(new UserTaskInvitationChanged(tenantId, taskId, committed.Status, committed.Revision), cancellationToken);

        // The raw token crosses only the dispatcher boundary and is never returned in the ordinary API
        // result. Dispatchers may persist an encrypted transient outbox entry for retry.
        await dispatcher.DispatchAsync(new UserTaskInvitationDelivery(
            identityGenerator.GenerateId(), tenantId, taskId, invitation.Id, request.VerifierName, token, expiresAt), cancellationToken);

        return new UserTaskInvitationIssueResult(ToSummary(invitation), request.OperationId);
    }

    public async Task<IReadOnlyCollection<UserTaskInvitationSummary>?> ListAsync(string tenantId, string taskId, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null || !await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.IssueInvitation, cancellationToken))
            return null;
        return task.Invitations.Select(ToSummary).ToArray();
    }

    public async Task<bool> RevokeAsync(string tenantId, string taskId, string invitationId, int expectedRevision, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null || !await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.IssueInvitation, cancellationToken))
            return false;
        if (!await repository.TryMutateAsync(tenantId, taskId, expectedRevision, current =>
            {
                var invitation = current.Invitations.FirstOrDefault(x => x.Id == invitationId);
                if (invitation == null || invitation.Status is UserTaskInvitationStatus.Revoked or UserTaskInvitationStatus.Consumed or UserTaskInvitationStatus.Expired)
                    return false;
                var index = current.Invitations.IndexOf(invitation);
                current.Invitations[index] = invitation with { Status = UserTaskInvitationStatus.Revoked, RevokedAt = clock.UtcNow };
                current.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), tenantId, taskId, current.Revision + 1,
                    "InvitationRevoked", clock.UtcNow, actor.Subject));
                return true;
            }, cancellationToken))
            return false;

        var committed = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (committed != null)
            await notifications.PublishAsync(new UserTaskInvitationChanged(tenantId, taskId, committed.Status, committed.Revision), cancellationToken);
        return true;
    }

    public async Task<UserTaskInvitationVerificationResultWithSession> VerifyAsync(UserTaskInvitationChallenge challenge, CancellationToken cancellationToken = default)
    {
        // Every failure below intentionally returns the same public code. Anonymous callers must not be
        // able to distinguish missing, expired, consumed, revoked, or invalid invitations.
        const string failure = "invitation-unavailable";
        if (string.IsNullOrWhiteSpace(challenge.Token) || !_tokens.TryGetValue(HashToken(challenge.Token), out var tokenEntry))
            return new UserTaskInvitationVerificationResultWithSession(false, FailureCode: failure);

        var tokenHash = HashToken(challenge.Token);
        var task = await repository.GetAsync(tokenEntry.TenantId, tokenEntry.TaskId, cancellationToken);
        var invitation = task?.Invitations.FirstOrDefault(x => x.Id == tokenEntry.InvitationId);
        if (task == null || invitation == null || invitation.ExpiresAt <= clock.UtcNow || invitation.Status is not (UserTaskInvitationStatus.Pending or UserTaskInvitationStatus.Dispatched))
        {
            _tokens.TryRemove(tokenHash, out _);
            return new UserTaskInvitationVerificationResultWithSession(false, FailureCode: failure);
        }

        var definition = task.InvitationDefinitions.FirstOrDefault(x => string.Equals(x.VerifierName, invitation.VerifierName, StringComparison.OrdinalIgnoreCase));
        var challengeResult = definition?.BearerOnly == true
            ? new UserTaskInvitationVerificationResult(true, Subject: null)
            : await verifier.VerifyAsync(challenge, cancellationToken);
        if (!challengeResult.Succeeded)
            return new UserTaskInvitationVerificationResultWithSession(false, FailureCode: failure);

        var subject = new ParticipantReference(task.TenantId, "guest", UserTaskParticipantType.User,
            challengeResult.Subject ?? $"invitation:{invitation.Id}");
        var verifiedAt = clock.UtcNow;
        var claimed = await repository.TryMutateAsync(task.TenantId, task.Id, task.Revision, current =>
        {
            var currentInvitation = current.Invitations.FirstOrDefault(x => x.Id == invitation.Id);
            if (currentInvitation == null || currentInvitation.ExpiresAt <= verifiedAt || currentInvitation.Status is not (UserTaskInvitationStatus.Pending or UserTaskInvitationStatus.Dispatched))
                return false;

            current.Assignee = subject;
            current.AssignedAt = verifiedAt;
            current.Status = UserTaskStatus.Assigned;
            var index = current.Invitations.IndexOf(currentInvitation);
            current.Invitations[index] = currentInvitation with
            {
                Status = UserTaskInvitationStatus.Verified,
                VerifiedAt = verifiedAt
            };
            var revokedSiblings = 0;
            for (var i = 0; i < current.Invitations.Count; i++)
            {
                var sibling = current.Invitations[i];
                if (sibling.Id != currentInvitation.Id && sibling.SiblingGroupId == currentInvitation.SiblingGroupId && sibling.Status is (UserTaskInvitationStatus.Pending or UserTaskInvitationStatus.Dispatched))
                {
                    current.Invitations[i] = sibling with { Status = UserTaskInvitationStatus.Revoked, RevokedAt = verifiedAt };
                    revokedSiblings++;
                }
            }
            current.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), current.TenantId, current.Id, current.Revision + 1,
                "InvitationVerified", verifiedAt, subject, Metadata: new Dictionary<string, object?> { ["revokedSiblingCount"] = revokedSiblings }));
            return true;
        }, cancellationToken);
        if (!claimed)
            return new UserTaskInvitationVerificationResultWithSession(false, FailureCode: failure);

        _tokens.TryRemove(tokenHash, out _);
        var verifiedInvitation = invitation with { Status = UserTaskInvitationStatus.Verified, VerifiedAt = verifiedAt };
        var session = await sessionIssuer.IssueAsync(verifiedInvitation, cancellationToken);
        return session.Succeeded
            ? new UserTaskInvitationVerificationResultWithSession(true, task.Id, session.Token, session.ExpiresAt)
            : new UserTaskInvitationVerificationResultWithSession(false, FailureCode: failure);
    }

    private static UserTaskInvitationSummary ToSummary(UserTaskInvitation invitation) => new(
        invitation.Id, invitation.TaskId, invitation.Recipient, invitation.Status, invitation.IssuedAt, invitation.ExpiresAt, invitation.VerifierName);

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record TokenEntry(string TenantId, string TaskId, string InvitationId);
}

public sealed class DefaultUserTaskInvitationDispatcher : IUserTaskInvitationDispatcher
{
    public Task DispatchAsync(UserTaskInvitationDelivery delivery, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>Development-safe guest session issuer. Providers should persist hashes and revocation state.</summary>
public sealed class DefaultUserTaskGuestSessionIssuer(ISystemClock clock) : IUserTaskGuestSessionIssuer
{
    private readonly ConcurrentDictionary<string, (string TaskId, DateTimeOffset ExpiresAt)> _sessions = new(StringComparer.Ordinal);

    public Task<GuestSessionResult> IssueAsync(UserTaskInvitation invitation, CancellationToken cancellationToken = default)
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var expiresAt = invitation.ExpiresAt;
        _sessions[HashToken(token)] = (invitation.TaskId, expiresAt);
        return Task.FromResult(new GuestSessionResult(true, token, expiresAt, TaskId: invitation.TaskId));
    }

    public bool IsValid(string taskId, string token)
    {
        var hash = HashToken(token);
        return _sessions.TryGetValue(hash, out var session) && session.TaskId == taskId && session.ExpiresAt > clock.UtcNow;
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

public sealed class DefaultUserTaskInvitationVerifier : IUserTaskInvitationVerifier
{
    public Task<UserTaskInvitationVerificationResult> VerifyAsync(UserTaskInvitationChallenge challenge, CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserTaskInvitationVerificationResult(false, "challenge-required"));
}
