using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Common;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Elsa.UserTasks.Services;

/// <summary>
/// In-process guest session store. Only the SHA-256 hash of a credential is retained, so a memory dump or a
/// log of this structure cannot be replayed against the API. Hosts running more than one replica should
/// register a shared-store implementation instead.
/// </summary>
public sealed class InMemoryUserTaskGuestSessionIssuer(ISystemClock clock, IOptions<UserTasksOptions> options) : IUserTaskGuestSessionIssuer
{
    private readonly ConcurrentDictionary<string, UserTaskGuestSession> _sessions = new(StringComparer.Ordinal);

    public Task<GuestSessionResult> IssueAsync(UserTaskInvitation invitation, ParticipantReference subject, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        // The session never outlives the invitation it came from, and never exceeds the host's own ceiling.
        var expiresAt = Min(invitation.ExpiresAt, now.Add(options.Value.GuestSessionLifetime));
        if (expiresAt <= now)
            return Task.FromResult(new GuestSessionResult(false, FailureCode: "session-unavailable"));

        var token = DefaultUserTaskInvitationService.Base64Url(RandomNumberGenerator.GetBytes(32));
        _sessions[DefaultUserTaskInvitationService.HashToken(token)] = new UserTaskGuestSession(
            invitation.TenantId, invitation.TaskId, invitation.Id, subject, invitation.AllowedActions.ToArray(), expiresAt);
        return Task.FromResult(new GuestSessionResult(true, token, expiresAt, TaskId: invitation.TaskId));
    }

    public Task<UserTaskGuestSession?> ResolveAsync(string credential, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(credential))
            return Task.FromResult<UserTaskGuestSession?>(null);

        var hash = DefaultUserTaskInvitationService.HashToken(credential);
        if (!_sessions.TryGetValue(hash, out var session))
            return Task.FromResult<UserTaskGuestSession?>(null);
        if (session.ExpiresAt > clock.UtcNow)
            return Task.FromResult<UserTaskGuestSession?>(session);

        _sessions.TryRemove(hash, out _);
        return Task.FromResult<UserTaskGuestSession?>(null);
    }

    public Task RevokeForTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken = default)
    {
        foreach (var entry in _sessions.Where(x => x.Value.TenantId == tenantId && x.Value.TaskId == taskId).ToArray())
            _sessions.TryRemove(entry.Key, out _);
        return Task.CompletedTask;
    }

    public Task RevokeForInvitationAsync(string tenantId, string invitationId, CancellationToken cancellationToken = default)
    {
        foreach (var entry in _sessions.Where(x => x.Value.TenantId == tenantId && x.Value.InvitationId == invitationId).ToArray())
            _sessions.TryRemove(entry.Key, out _);
        return Task.CompletedTask;
    }

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) => left <= right ? left : right;
}

/// <summary>
/// Fixed-window limiter for the anonymous invitation surface. Counters are keyed by a caller partition
/// (normally the remote address) and never by the token, so probing many tokens from one host still
/// consumes one budget.
/// </summary>
public sealed class SlidingWindowUserTaskInvitationRateLimiter(ISystemClock clock, IOptions<UserTasksOptions> options) : IUserTaskInvitationRateLimiter
{
    private readonly ConcurrentDictionary<string, Window> _windows = new(StringComparer.Ordinal);

    public ValueTask<bool> TryAcquireAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (settings.AnonymousRateLimit <= 0)
            return ValueTask.FromResult(true);

        var now = clock.UtcNow;
        var allowed = true;
        _windows.AddOrUpdate(partitionKey,
            _ => new Window(now, 1),
            (_, existing) =>
            {
                if (now - existing.StartedAt >= settings.AnonymousRateLimitWindow)
                    return new Window(now, 1);
                allowed = existing.Count < settings.AnonymousRateLimit;
                return existing with { Count = existing.Count + 1 };
            });

        // Opportunistic eviction keeps the dictionary bounded without a dedicated timer.
        if (_windows.Count > 10_000)
        {
            foreach (var stale in _windows.Where(x => now - x.Value.StartedAt >= settings.AnonymousRateLimitWindow).Take(1_000).ToArray())
                _windows.TryRemove(stale.Key, out _);
        }

        return ValueTask.FromResult(allowed);
    }

    private sealed record Window(DateTimeOffset StartedAt, int Count);
}

/// <summary>
/// Transient outbox for invitation secrets awaiting delivery. Entries are encrypted with ASP.NET Core Data
/// Protection so the plaintext token exists only inside a dispatch attempt, and are dropped once delivery
/// succeeds, the retry schedule is exhausted, or the invitation expires.
/// </summary>
public sealed class InMemoryUserTaskInvitationOutbox(
    IDataProtectionProvider dataProtectionProvider,
    ISystemClock clock,
    IOptions<UserTasksOptions> options) : IUserTaskInvitationOutbox
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Elsa.UserTasks.InvitationDelivery.v1");
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public Task EnqueueAsync(UserTaskInvitationDelivery delivery, CancellationToken cancellationToken = default)
    {
        _entries[delivery.Id] = new Entry(
            delivery.Id, delivery.TenantId, delivery.TaskId, delivery.InvitationId, delivery.DispatcherName,
            delivery.Recipient, _protector.Protect(JsonSerializer.Serialize(delivery.Token)), delivery.ExpiresAt,
            delivery.Attempt, delivery.NotBefore ?? clock.UtcNow);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<UserTaskInvitationDelivery>> DequeueDueAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        foreach (var expired in _entries.Where(x => x.Value.ExpiresAt <= now).ToArray())
            _entries.TryRemove(expired.Key, out _);

        var due = _entries.Values
            .Where(x => x.NotBefore <= now)
            .OrderBy(x => x.NotBefore)
            .Take(Math.Max(1, maxCount))
            .Select(Unprotect)
            .Where(x => x != null)
            .Select(x => x!)
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<UserTaskInvitationDelivery>>(due);
    }

    public Task CompleteAsync(string deliveryId, CancellationToken cancellationToken = default)
    {
        _entries.TryRemove(deliveryId, out _);
        return Task.CompletedTask;
    }

    public Task RescheduleAsync(string deliveryId, DateTimeOffset notBefore, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(deliveryId, out var entry))
            return Task.CompletedTask;

        var attempt = entry.Attempt + 1;
        // Abandon rather than retry forever: an undeliverable secret should expire, and a manager can reissue.
        if (attempt > options.Value.InvitationDeliveryRetryDelays.Count)
            _entries.TryRemove(deliveryId, out _);
        else
            _entries[deliveryId] = entry with { Attempt = attempt, NotBefore = notBefore };
        return Task.CompletedTask;
    }

    private UserTaskInvitationDelivery? Unprotect(Entry entry)
    {
        try
        {
            var token = JsonSerializer.Deserialize<string>(_protector.Unprotect(entry.ProtectedToken));
            return token == null
                ? null
                : new UserTaskInvitationDelivery(entry.Id, entry.TenantId, entry.TaskId, entry.InvitationId, entry.DispatcherName, token, entry.ExpiresAt)
                {
                    Recipient = entry.Recipient,
                    Attempt = entry.Attempt,
                    NotBefore = entry.NotBefore
                };
        }
        catch (CryptographicException)
        {
            // A rotated or unavailable key makes the secret unrecoverable. Drop it instead of surfacing it.
            _entries.TryRemove(entry.Id, out _);
            return null;
        }
    }

    private sealed record Entry(
        string Id,
        string TenantId,
        string TaskId,
        string InvitationId,
        string DispatcherName,
        string? Recipient,
        string ProtectedToken,
        DateTimeOffset ExpiresAt,
        int Attempt,
        DateTimeOffset NotBefore);
}

/// <summary>
/// Resolves a presented guest credential into a task-scoped actor. The actor carries only the permissions a
/// guest needs, plus the task and action allowlist the policy layer enforces.
/// </summary>
public sealed class UserTaskGuestActorResolver(IUserTaskGuestSessionIssuer sessions)
{
    public const string CredentialScheme = "UserTaskSession";

    public async Task<UserTaskActor?> ResolveAsync(string? credential, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(credential))
            return null;
        if (await sessions.ResolveAsync(credential, cancellationToken) is not { } session)
            return null;

        return new UserTaskActor(session.Subject, [], session.Subject.DisplayName)
        {
            IsManager = false,
            Permissions = new HashSet<string>([Permissions.UserTasksPermissions.Read, Permissions.UserTasksPermissions.Complete], StringComparer.OrdinalIgnoreCase),
            GuestTaskId = session.TaskId,
            GuestAllowedActions = new HashSet<string>(session.AllowedActions, StringComparer.OrdinalIgnoreCase)
        };
    }

    /// <summary>Extracts the credential from an <c>Authorization: UserTaskSession &lt;token&gt;</c> header value.</summary>
    public static string? ReadCredential(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return null;
        var value = authorizationHeader.Trim();
        return value.StartsWith(CredentialScheme + " ", StringComparison.OrdinalIgnoreCase)
            ? value[(CredentialScheme.Length + 1)..].Trim()
            : null;
    }
}
