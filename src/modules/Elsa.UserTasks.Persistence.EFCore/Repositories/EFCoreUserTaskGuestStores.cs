using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common;
using Elsa.Persistence.EFCore;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Elsa.UserTasks.Persistence.EFCore.Repositories;

internal static class UserTaskSecretHashing
{
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>
/// Durable guest session store. Only the credential hash is written, and every read re-checks expiry and
/// revocation so a session cannot outlive its task across a restart or a failover.
/// </summary>
public sealed class EFCoreUserTaskGuestSessionIssuer(
    Store<UserTasksElsaDbContext, UserTaskGuestSessionRecord> store,
    ISystemClock clock,
    IOptions<UserTasksOptions> options) : IUserTaskGuestSessionIssuer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task<GuestSessionResult> IssueAsync(UserTaskInvitation invitation, ParticipantReference subject, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var expiresAt = invitation.ExpiresAt <= now.Add(options.Value.GuestSessionLifetime) ? invitation.ExpiresAt : now.Add(options.Value.GuestSessionLifetime);
        if (expiresAt <= now)
            return new(false, FailureCode: "session-unavailable");

        var token = UserTaskSecretHashing.CreateToken();
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        dbContext.UserTaskGuestSessions.Add(new()
        {
            TenantId = invitation.TenantId,
            TaskId = invitation.TaskId,
            InvitationId = invitation.Id,
            SessionTokenHash = UserTaskSecretHashing.Hash(token),
            GuestParticipantJson = JsonSerializer.Serialize(subject, JsonOptions),
            CapabilitiesJson = JsonSerializer.Serialize(invitation.AllowedActions, JsonOptions),
            IssuedAt = now,
            ExpiresAt = expiresAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(true, token, expiresAt, TaskId: invitation.TaskId);
    }

    public async Task<UserTaskGuestSession?> ResolveAsync(string credential, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(credential))
            return null;

        var hash = UserTaskSecretHashing.Hash(credential);
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var now = clock.UtcNow;
        var row = await dbContext.UserTaskGuestSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SessionTokenHash == hash && x.RevokedAt == null && x.ExpiresAt > now, cancellationToken);
        if (row is null)
            return null;

        var subject = JsonSerializer.Deserialize<ParticipantReference>(row.GuestParticipantJson, JsonOptions);
        if (subject is null)
            return null;

        var actions = JsonSerializer.Deserialize<List<string>>(row.CapabilitiesJson, JsonOptions) ?? [];
        return new(row.TenantId, row.TaskId, row.InvitationId, subject, actions, row.ExpiresAt);
    }

    public async Task RevokeForTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        await dbContext.UserTaskGuestSessions
            .Where(x => x.TenantId == tenantId && x.TaskId == taskId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.RevokedAt, clock.UtcNow), cancellationToken);
    }

    public async Task RevokeForInvitationAsync(string tenantId, string invitationId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        await dbContext.UserTaskGuestSessions
            .Where(x => x.TenantId == tenantId && x.InvitationId == invitationId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.RevokedAt, clock.UtcNow), cancellationToken);
    }
}

/// <summary>
/// Durable invitation-delivery outbox. Tokens are encrypted with ASP.NET Core Data Protection before they
/// reach the database, so a table dump never yields a usable invitation link.
/// </summary>
public sealed class EFCoreUserTaskInvitationOutbox(
    Store<UserTasksElsaDbContext, UserTaskInvitationDeliveryRecord> store,
    IDataProtectionProvider dataProtectionProvider,
    ISystemClock clock,
    IOptions<UserTasksOptions> options) : IUserTaskInvitationOutbox
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Elsa.UserTasks.InvitationDelivery.v1");

    public async Task EnqueueAsync(UserTaskInvitationDelivery delivery, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        dbContext.UserTaskInvitationDeliveries.Add(new()
        {
            Id = delivery.Id,
            TenantId = delivery.TenantId,
            TaskId = delivery.TaskId,
            InvitationId = delivery.InvitationId,
            DispatcherProvider = delivery.DispatcherName,
            EncryptedToken = _protector.Protect(delivery.Token),
            DeliveryMetadataJson = delivery.Recipient == null ? null : JsonSerializer.Serialize(new { delivery.Recipient }),
            Status = UserTaskPersistenceDeliveryStatus.Pending,
            AvailableAt = delivery.NotBefore ?? clock.UtcNow,
            ExpiresAt = delivery.ExpiresAt,
            CreatedAt = clock.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserTaskInvitationDelivery>> DequeueDueAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var now = clock.UtcNow;
        // Expired secrets are dropped, never delivered late.
        await dbContext.UserTaskInvitationDeliveries.Where(x => x.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken);

        var rows = await dbContext.UserTaskInvitationDeliveries.AsNoTracking()
            .Where(x => x.Status == UserTaskPersistenceDeliveryStatus.Pending && x.AvailableAt <= now)
            .OrderBy(x => x.AvailableAt)
            .Take(Math.Max(1, maxCount))
            .ToListAsync(cancellationToken);

        var deliveries = new List<UserTaskInvitationDelivery>(rows.Count);
        foreach (var row in rows)
        {
            string token;
            try
            {
                token = _protector.Unprotect(row.EncryptedToken);
            }
            catch (CryptographicException)
            {
                // A rotated or unavailable key makes the secret unrecoverable; drop it so a manager reissues.
                await dbContext.UserTaskInvitationDeliveries.Where(x => x.Id == row.Id).ExecuteDeleteAsync(cancellationToken);
                continue;
            }

            deliveries.Add(new(row.Id, row.TenantId, row.TaskId, row.InvitationId, row.DispatcherProvider, token, row.ExpiresAt)
            {
                Attempt = row.Attempts,
                NotBefore = row.AvailableAt
            });
        }

        return deliveries;
    }

    public async Task CompleteAsync(string deliveryId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        // Delivery succeeded, so the encrypted secret has no further purpose and is removed outright.
        await dbContext.UserTaskInvitationDeliveries.Where(x => x.Id == deliveryId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task RescheduleAsync(string deliveryId, DateTimeOffset notBefore, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var row = await dbContext.UserTaskInvitationDeliveries.FirstOrDefaultAsync(x => x.Id == deliveryId, cancellationToken);
        if (row is null)
            return;

        row.Attempts += 1;
        if (row.Attempts > options.Value.InvitationDeliveryRetryDelays.Count)
        {
            dbContext.UserTaskInvitationDeliveries.Remove(row);
        }
        else
        {
            row.AvailableAt = notBefore;
            row.LastErrorCode = "dispatch-failed";
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
