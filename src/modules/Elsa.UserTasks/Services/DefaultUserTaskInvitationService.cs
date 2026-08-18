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
/// Implements the invitation protocol. Secrets never leave this service in plaintext except across the
/// dispatcher boundary: only a SHA-256 hash is persisted on the task, and verification resolves the owning
/// task by that hash so the flow works across restarts and behind any persistence provider.
/// </summary>
public sealed class DefaultUserTaskInvitationService(
    IUserTaskRepository repository,
    IUserTaskAccessPolicy accessPolicy,
    IUserTaskInvitationOutbox outbox,
    IUserTaskInvitationVerifier verifier,
    IUserTaskGuestSessionIssuer sessionIssuer,
    IUserTaskNotificationSink notifications,
    IIdentityGenerator identityGenerator,
    ISystemClock clock,
    IOptions<UserTasksOptions> options) : IUserTaskInvitationService
{
    /// <summary>The single public failure code. Callers must not be able to tell these cases apart.</summary>
    private const string GenericFailure = "invitation-unavailable";

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
            SiblingGroupId: siblingGroupId)
        {
            // Pinned at issuance: a later workflow definition change cannot widen an outstanding link.
            AllowedActions = actions
        };

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

        var committed = await repository.GetAsync(tenantId, taskId, cancellationToken) ?? task;
        await notifications.PublishAsync(new UserTaskInvitationChanged(tenantId, taskId, committed.Status, committed.Revision), cancellationToken);

        // The raw token crosses only the dispatcher boundary. It is parked in the encrypted outbox first so
        // a dispatcher failure can be retried without ever re-deriving the secret or returning it to the API.
        await outbox.EnqueueAsync(new UserTaskInvitationDelivery(
            identityGenerator.GenerateId(), tenantId, taskId, invitation.Id, definition.VerifierName, token, expiresAt)
        {
            Recipient = request.Recipient
        }, cancellationToken);

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

    public async Task<UserTaskInvitationChallengeDescriptor> DescribeAsync(string token, CancellationToken cancellationToken = default)
    {
        // Copy is identical for every token. Only the challenge shape differs, and only for a token the
        // caller already holds, so this cannot be used to probe whether an unknown token exists.
        var resolved = await ResolveOpenInvitationAsync(token, cancellationToken);
        var bearerOnly = resolved is { } match && FindDefinition(match.Task, match.Invitation)?.BearerOnly == true;
        return bearerOnly
            ? new UserTaskInvitationChallengeDescriptor("bearer", "Open this task to continue.", RequiresCode: false)
            : new UserTaskInvitationChallengeDescriptor("code", "Enter the verification code you were sent to continue.", RequiresCode: true);
    }

    public async Task<UserTaskInvitationVerificationResultWithSession> VerifyAsync(UserTaskInvitationChallenge challenge, CancellationToken cancellationToken = default)
    {
        if (await ResolveOpenInvitationAsync(challenge.Token, cancellationToken) is not { } resolved)
            return Failed();

        var (task, invitation) = resolved;
        var definition = FindDefinition(task, invitation);
        var challengeResult = definition?.BearerOnly == true
            ? new UserTaskInvitationVerificationResult(true, Subject: null)
            : await verifier.VerifyAsync(challenge, cancellationToken);
        if (!challengeResult.Succeeded)
            return Failed();

        var subject = new ParticipantReference(task.TenantId, "guest", UserTaskParticipantType.User,
            challengeResult.Subject ?? $"invitation:{invitation.Id}");
        var verifiedAt = clock.UtcNow;

        // Claiming, consuming, and sibling revocation happen inside one compare-and-swap. A second holder
        // racing the same sibling group therefore loses and receives the same generic failure.
        var claimed = await repository.TryMutateAsync(task.TenantId, task.Id, task.Revision, current =>
        {
            var currentInvitation = current.Invitations.FirstOrDefault(x => x.Id == invitation.Id);
            if (currentInvitation == null || currentInvitation.ExpiresAt <= verifiedAt || currentInvitation.Status is not (UserTaskInvitationStatus.Pending or UserTaskInvitationStatus.Dispatched))
                return false;
            if (current.IsTerminal || current.Status is UserTaskStatus.Completing or UserTaskStatus.TimingOut or UserTaskStatus.Cancelling)
                return false;

            current.Assignee = subject;
            current.AssignedAt = verifiedAt;
            current.Status = UserTaskStatus.Assigned;
            var index = current.Invitations.IndexOf(currentInvitation);
            current.Invitations[index] = currentInvitation with
            {
                Status = UserTaskInvitationStatus.Consumed,
                VerifiedAt = verifiedAt,
                ConsumedAt = verifiedAt
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
            // The challenge response and the secret itself are never written to the audit trail.
            current.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), current.TenantId, current.Id, current.Revision + 1,
                "InvitationVerified", verifiedAt, subject, Metadata: new Dictionary<string, object?> { ["revokedSiblingCount"] = revokedSiblings }));
            return true;
        }, cancellationToken);
        if (!claimed)
            return Failed();

        var consumed = invitation with { Status = UserTaskInvitationStatus.Consumed, VerifiedAt = verifiedAt, ConsumedAt = verifiedAt };
        var session = await sessionIssuer.IssueAsync(consumed, subject, cancellationToken);
        return session.Succeeded
            ? new UserTaskInvitationVerificationResultWithSession(true, task.Id, session.Token, session.ExpiresAt)
            : Failed();
    }

    private async Task<(UserTask Task, UserTaskInvitation Invitation)?> ResolveOpenInvitationAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;
        var match = await repository.FindByInvitationTokenHashAsync(HashToken(token), cancellationToken);
        if (match is not { } found)
            return null;
        var invitation = found.Invitation;
        return invitation.ExpiresAt <= clock.UtcNow || invitation.Status is not (UserTaskInvitationStatus.Pending or UserTaskInvitationStatus.Dispatched)
            ? null
            : found;
    }

    private static UserTaskInvitationDefinition? FindDefinition(UserTask task, UserTaskInvitation invitation) =>
        task.InvitationDefinitions.FirstOrDefault(x => string.Equals(x.VerifierName, invitation.VerifierName, StringComparison.OrdinalIgnoreCase));

    private static UserTaskInvitationVerificationResultWithSession Failed() => new(false, FailureCode: GenericFailure);

    private static UserTaskInvitationSummary ToSummary(UserTaskInvitation invitation) => new(
        invitation.Id, invitation.TaskId, invitation.Recipient, invitation.Status, invitation.IssuedAt, invitation.ExpiresAt, invitation.VerifierName);

    internal static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    internal static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>A dispatcher that drops deliveries. Hosts replace it with an email, SMS, or webhook dispatcher.</summary>
public sealed class NullUserTaskInvitationDispatcher : IUserTaskInvitationDispatcher
{
    public Task DispatchAsync(UserTaskInvitationDelivery delivery, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>
/// The default verifier refuses every challenge. A host that enables guest invitations must register a real
/// verifier; failing closed is preferable to accepting any bearer who holds a link.
/// </summary>
public sealed class DefaultUserTaskInvitationVerifier : IUserTaskInvitationVerifier
{
    public Task<UserTaskInvitationVerificationResult> VerifyAsync(UserTaskInvitationChallenge challenge, CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserTaskInvitationVerificationResult(false, "challenge-required"));
}
