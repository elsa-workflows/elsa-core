using System.Text.Json;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

/// <summary>Database-owned lifecycle state. Every mutation is a scoped conditional update for cross-worker CAS.</summary>
public sealed class EFCoreConnectionLifecycleStore(IDbContextFactory<ConnectionsElsaDbContext> dbContextFactory) : IConnectionLifecycleStore, IConnectionDueCandidateStore
{
    private const int MaximumDueCandidatePageSize = 500;

    public async Task<ConnectionDueCandidatePage> FindDueCandidatesAsync(
        string tenantId,
        string environmentId,
        DateTimeOffset now,
        int pageSize,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(environmentId))
            throw new ArgumentException("Tenant and environment scope are required.");
        if (pageSize is < 1 or > MaximumDueCandidatePageSize)
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"Page size must be between 1 and {MaximumDueCandidatePageSize}.");

        // SQLite persists DateTimeOffset values with their original offset and compares their textual form.
        // Normalize the query boundary to match the UTC invariant applied by this store on credential metadata writes.
        now = now.ToUniversalTime();
        var after = cursor == null ? null : DecodeCursor(cursor);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var idCollation = db.Database.ProviderName switch
        {
            "Npgsql.EntityFrameworkCore.PostgreSQL" => "C",
            "Microsoft.EntityFrameworkCore.Sqlite" => "BINARY",
            _ => throw new NotSupportedException($"Due-candidate ordering is not configured for provider '{db.Database.ProviderName}'.")
        };

        var oauth = db.Connections.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.Status == ConnectionStatus.Active && x.CredentialKind == ConnectionCredentialKind.OAuth &&
                        x.CredentialExpiresAt != null && x.CredentialExpiresAt <= now)
            .Select(x => new { TenantId = x.TenantId!, x.EnvironmentId, ConnectionId = x.Id,
                Kind = ConnectionDueCandidateKind.OAuthRefresh, CandidateId = x.Id, DueAt = x.CredentialExpiresAt!.Value });

        var expiredOperations = db.Connections.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.OperationLeaseExpiresAt != null && x.OperationLeaseExpiresAt <= now &&
                        (x.OperationStatus == CredentialOperationStatus.Claimed ||
                         x.OperationStatus == CredentialOperationStatus.ProviderCallStarted ||
                         x.OperationStatus == CredentialOperationStatus.CredentialReceived ||
                         x.OperationStatus == CredentialOperationStatus.Staged))
            .Select(x => new { TenantId = x.TenantId!, x.EnvironmentId, ConnectionId = x.Id,
                Kind = ConnectionDueCandidateKind.ExpiredConnectionOperation, CandidateId = x.OperationId ?? x.Id, DueAt = x.OperationLeaseExpiresAt!.Value });

        var recovery = db.Connections.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        (x.Status == ConnectionStatus.RecoveryRequired || x.OperationStatus == CredentialOperationStatus.RecoveryRequired))
            .Select(x => new { TenantId = x.TenantId!, x.EnvironmentId, ConnectionId = x.Id,
                Kind = ConnectionDueCandidateKind.RecoveryRequired, CandidateId = x.OperationId ?? x.Id, DueAt = DateTimeOffset.MinValue });

        var cleanups = db.GenerationCleanups.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.Status == ConnectionGenerationCleanupStatus.Deleting &&
                        (x.LeaseExpiresAt == null || x.LeaseExpiresAt <= now))
            .Select(x => new { x.TenantId, x.EnvironmentId, ConnectionId = x.ConnectionId,
                Kind = ConnectionDueCandidateKind.GenerationCleanup, CandidateId = x.GenerationId, DueAt = x.LeaseExpiresAt ?? DateTimeOffset.MinValue });

        var offboarding = db.OffboardingOperations.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        (x.Status == ConnectionOffboardingOperationStatus.Pending && x.CreatedAt <= now ||
                         x.Status == ConnectionOffboardingOperationStatus.RetryScheduled && (x.NextAttemptAt == null || x.NextAttemptAt <= now) ||
                         x.Status == ConnectionOffboardingOperationStatus.UnknownOutcome && (x.NextAttemptAt == null || x.NextAttemptAt <= now) ||
                         (x.Status == ConnectionOffboardingOperationStatus.Claimed || x.Status == ConnectionOffboardingOperationStatus.ProviderCallStarted) &&
                         x.LeaseExpiresAt != null && x.LeaseExpiresAt <= now))
            .Select(x => new { x.TenantId, x.EnvironmentId, ConnectionId = x.ConnectionId,
                Kind = ConnectionDueCandidateKind.Offboarding, CandidateId = x.Id,
                DueAt = x.Status == ConnectionOffboardingOperationStatus.Pending ? x.CreatedAt :
                    x.Status == ConnectionOffboardingOperationStatus.RetryScheduled || x.Status == ConnectionOffboardingOperationStatus.UnknownOutcome
                        ? x.NextAttemptAt ?? x.CreatedAt : x.LeaseExpiresAt!.Value });

        var query = oauth.Concat(expiredOperations).Concat(recovery).Concat(cleanups).Concat(offboarding);
        if (after != null)
        {
            query = query.Where(x => x.DueAt > after.DueAt ||
                x.DueAt == after.DueAt && ((int)x.Kind > after.Kind ||
                (int)x.Kind == after.Kind && (string.Compare(EF.Functions.Collate(x.ConnectionId, idCollation), after.ConnectionId) > 0 ||
                EF.Functions.Collate(x.ConnectionId, idCollation) == after.ConnectionId &&
                string.Compare(EF.Functions.Collate(x.CandidateId, idCollation), after.CandidateId) > 0)));
        }

        var ordered = await query.OrderBy(x => x.DueAt).ThenBy(x => x.Kind)
            .ThenBy(x => EF.Functions.Collate(x.ConnectionId, idCollation))
            .ThenBy(x => EF.Functions.Collate(x.CandidateId, idCollation))
            .Take(pageSize + 1)
            .Select(x => new ConnectionDueCandidate(x.TenantId, x.EnvironmentId, x.ConnectionId, x.Kind, x.CandidateId, x.DueAt))
            .ToListAsync(cancellationToken);
        var hasMore = ordered.Count > pageSize;
        var items = ordered.Take(pageSize).ToArray();
        var nextCursor = hasMore ? EncodeCursor(ordered[pageSize - 1]) : null;
        return new ConnectionDueCandidatePage(items, nextCursor);
    }

    private static string EncodeCursor(ConnectionDueCandidate candidate)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new DueCandidateCursor(candidate.DueAt, (int)candidate.Kind,
            candidate.ConnectionId, candidate.CandidateId));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static DueCandidateCursor DecodeCursor(string cursor)
    {
        try
        {
            if (cursor.Length is 0 or > 2048 || cursor.Any(character =>
                    !(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_')))
                throw new FormatException();
            var value = cursor.Replace('-', '+').Replace('_', '/');
            value += new string('=', (4 - value.Length % 4) % 4);
            var decoded = JsonSerializer.Deserialize<DueCandidateCursor>(Convert.FromBase64String(value))
                          ?? throw new FormatException();
            if (decoded.DueAt.Offset != TimeSpan.Zero || !Enum.IsDefined(typeof(ConnectionDueCandidateKind), decoded.Kind) ||
                string.IsNullOrWhiteSpace(decoded.ConnectionId) || string.IsNullOrWhiteSpace(decoded.CandidateId))
                throw new FormatException();
            return decoded;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw new ArgumentException("The due-candidate cursor is invalid.", nameof(cursor), exception);
        }
    }

    private sealed record DueCandidateCursor(DateTimeOffset DueAt, int Kind, string ConnectionId, string CandidateId);

    public async Task CreateAsync(IntegrationConnection connection, CancellationToken cancellationToken = default)
    {
        connection.CredentialExpiresAt = connection.CredentialExpiresAt?.ToUniversalTime();
        connection.StagedCredentialExpiresAt = connection.StagedCredentialExpiresAt?.ToUniversalTime();
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Connections.Add(connection);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IntegrationConnection?> FindAsync(string id, string tenantId, string environmentId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ConnectionGenerationCleanup?> FindGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.GenerationCleanups.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ConnectionId == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId && x.GenerationId == generationId, cancellationToken);
    }

    public async Task<ConnectionGenerationCleanup?> TryClaimGenerationCleanupAsync(
        string connectionId,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string generationId,
        DateTimeOffset now,
        DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var cleanup = await db.GenerationCleanups.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ConnectionId == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId && x.GenerationId == generationId, cancellationToken);

        if (cleanup?.Status == ConnectionGenerationCleanupStatus.Deleted ||
            cleanup != null && cleanup.Status == ConnectionGenerationCleanupStatus.Deleting && cleanup.LeaseExpiresAt.HasValue && cleanup.LeaseExpiresAt.Value > now)
        {
            return null;
        }

        var rows = await Scoped(db, connectionId, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision &&
                        (x.CurrentGenerationId != generationId &&
                         (x.OperationStatus == CredentialOperationStatus.None || x.OperationStatus == CredentialOperationStatus.Completed ||
                          (x.OperationSourceGenerationId != generationId && x.PlannedGenerationId != generationId && x.StagedGenerationId != generationId)) ||
                         (x.Status == ConnectionStatus.Disconnected && x.CurrentGenerationId == generationId &&
                          (x.OperationStatus == CredentialOperationStatus.None || x.OperationStatus == CredentialOperationStatus.Completed))) &&
                        !db.OffboardingOperations.Any(operation => operation.ConnectionId == connectionId && operation.TenantId == tenantId &&
                            operation.EnvironmentId == environmentId && operation.GenerationId == generationId &&
                            operation.Status != ConnectionOffboardingOperationStatus.Completed &&
                            operation.Status != ConnectionOffboardingOperationStatus.TerminalFailure))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.CurrentSecretName, x => x.CurrentGenerationId == generationId ? null : x.CurrentSecretName)
                .SetProperty(x => x.CurrentGenerationId, x => x.CurrentGenerationId == generationId ? null : x.CurrentGenerationId)
                .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);

        if (rows != 1)
        {
            return null;
        }

        if (cleanup == null)
        {
            cleanup = new ConnectionGenerationCleanup
            {
                ConnectionId = connectionId,
                TenantId = tenantId,
                EnvironmentId = environmentId,
                GenerationId = generationId,
                Status = ConnectionGenerationCleanupStatus.Deleting,
                Fence = 1,
                LeaseExpiresAt = leaseExpiresAt
            };
            db.GenerationCleanups.Add(cleanup);
        }
        else
        {
            var updated = await db.GenerationCleanups
                .Where(x => x.ConnectionId == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                            x.GenerationId == generationId && x.Status == ConnectionGenerationCleanupStatus.Deleting &&
                            x.Fence == cleanup.Fence && x.LeaseExpiresAt <= now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Fence, x => x.Fence + 1)
                    .SetProperty(x => x.LeaseExpiresAt, leaseExpiresAt), cancellationToken);
            if (updated != 1)
            {
                return null;
            }
            cleanup.Fence++;
            cleanup.LeaseExpiresAt = leaseExpiresAt;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return cleanup;
    }

    public async Task<IntegrationConnection?> TryDisconnectAndRecordAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        ConnectionOffboardingOperation operation,
        CancellationToken cancellationToken = default)
    {
        if (expectedRevision == long.MaxValue)
        {
            return null;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var rows = await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision &&
                        (x.Status == ConnectionStatus.Active || x.Status == ConnectionStatus.RecoveryRequired || x.Status == ConnectionStatus.Disconnected) &&
                        !db.OffboardingOperations.Any(existing => existing.Id == operation.Id && existing.TenantId == tenantId &&
                            existing.EnvironmentId == environmentId && existing.ConnectionId == id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionStatus.Disconnected)
                .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);
        if (rows != 1)
        {
            var existing = await db.OffboardingOperations.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == operation.Id && x.TenantId == tenantId && x.EnvironmentId == environmentId && x.ConnectionId == id, cancellationToken);
            if (existing == null)
            {
                return null;
            }

            return await Scoped(db, id, tenantId, environmentId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        }

        db.OffboardingOperations.Add(operation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ConnectionOffboardingOperation?> FindOffboardingOperationAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.OffboardingOperations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == operationId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                                       x.ConnectionId == connectionId, cancellationToken);
    }

    public async Task<ConnectionOffboardingOperation?> FindNextOffboardingOperationAsync(
        string tenantId,
        string environmentId,
        string connectionId,
        DateTimeOffset now,
        bool stableRevocationIdIdempotency,
        bool stableUninstallIdIdempotency,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.OffboardingOperations.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EnvironmentId == environmentId && x.ConnectionId == connectionId &&
                        x.Kind != ConnectionOffboardingOperationKind.LocalDisconnect &&
                        (x.Status == ConnectionOffboardingOperationStatus.Pending ||
                         (x.Status == ConnectionOffboardingOperationStatus.RetryScheduled &&
                          (x.NextAttemptAt == null || x.NextAttemptAt <= now)) ||
                         (x.Status == ConnectionOffboardingOperationStatus.UnknownOutcome &&
                          ((x.Kind == ConnectionOffboardingOperationKind.TokenPairRevocation &&
                            (!stableRevocationIdIdempotency || x.NextAttemptAt == null || x.NextAttemptAt <= now)) ||
                           (x.Kind == ConnectionOffboardingOperationKind.InstallationUninstall &&
                            (!stableUninstallIdIdempotency || x.NextAttemptAt == null || x.NextAttemptAt <= now)))) ||
                         ((x.Status == ConnectionOffboardingOperationStatus.Claimed || x.Status == ConnectionOffboardingOperationStatus.ProviderCallStarted) &&
                          x.LeaseExpiresAt != null && x.LeaseExpiresAt <= now)))
            .OrderBy(x => x.Status == ConnectionOffboardingOperationStatus.UnknownOutcome || x.Status == ConnectionOffboardingOperationStatus.ProviderCallStarted ? 1 : 0)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ConnectionOffboardingOperation?> TryQueueOffboardingOperationAsync(
        long expectedConnectionRevision,
        ConnectionOffboardingOperation operation,
        CancellationToken cancellationToken = default)
    {
        if (expectedConnectionRevision == long.MaxValue)
        {
            return null;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var rows = await Scoped(db, operation.ConnectionId, operation.TenantId, operation.EnvironmentId)
            .Where(x => x.Revision == expectedConnectionRevision && x.Status == ConnectionStatus.Disconnected &&
                        (x.OperationStatus == CredentialOperationStatus.None || x.OperationStatus == CredentialOperationStatus.Completed) &&
                        !db.OffboardingOperations.Any(existing => existing.Id == operation.Id && existing.TenantId == operation.TenantId &&
                            existing.EnvironmentId == operation.EnvironmentId && existing.ConnectionId == operation.ConnectionId) &&
                        (operation.GenerationId == null || !db.GenerationCleanups.Any(cleanup =>
                            cleanup.ConnectionId == operation.ConnectionId && cleanup.TenantId == operation.TenantId &&
                            cleanup.EnvironmentId == operation.EnvironmentId && cleanup.GenerationId == operation.GenerationId)))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);
        if (rows != 1)
        {
            return await db.OffboardingOperations.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == operation.Id && x.TenantId == operation.TenantId &&
                                           x.EnvironmentId == operation.EnvironmentId && x.ConnectionId == operation.ConnectionId, cancellationToken);
        }

        db.OffboardingOperations.Add(operation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return operation;
    }

    public async Task<ConnectionOffboardingOperation?> TryClaimOffboardingOperationAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long expectedFence,
        DateTimeOffset now,
        DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken = default)
    {
        if (expectedFence == long.MaxValue)
        {
            return null;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.OffboardingOperations
            .Where(x => x.Id == operationId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.ConnectionId == connectionId && x.Fence == expectedFence &&
                        (x.Status == ConnectionOffboardingOperationStatus.Pending ||
                         ((x.Status == ConnectionOffboardingOperationStatus.RetryScheduled || x.Status == ConnectionOffboardingOperationStatus.UnknownOutcome) &&
                          (x.NextAttemptAt == null || x.NextAttemptAt <= now)) ||
                         ((x.Status == ConnectionOffboardingOperationStatus.Claimed || x.Status == ConnectionOffboardingOperationStatus.ProviderCallStarted) &&
                          x.LeaseExpiresAt != null && x.LeaseExpiresAt <= now)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionOffboardingOperationStatus.Claimed)
                .SetProperty(x => x.Fence, x => x.Fence + 1)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                .SetProperty(x => x.UpdatedAt, now)
                .SetProperty(x => x.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LeaseExpiresAt, leaseExpiresAt), cancellationToken);
        if (rows != 1)
        {
            return null;
        }

        return await db.OffboardingOperations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == operationId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                                       x.ConnectionId == connectionId && x.Fence == expectedFence + 1, cancellationToken);
    }

    public async Task<bool> TryStartOffboardingProviderCallAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long fence,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.OffboardingOperations
            .Where(x => x.Id == operationId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.ConnectionId == connectionId && x.Fence == fence && x.Status == ConnectionOffboardingOperationStatus.Claimed &&
                        x.LeaseExpiresAt != null && x.LeaseExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionOffboardingOperationStatus.ProviderCallStarted)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken) == 1;
    }

    public async Task<bool> TryMarkOffboardingOutcomeUnknownIfLeaseExpiredAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long fence,
        DateTimeOffset now,
        string safeErrorCode,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.OffboardingOperations
            .Where(x => x.Id == operationId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.ConnectionId == connectionId && x.Fence == fence &&
                        x.Status == ConnectionOffboardingOperationStatus.ProviderCallStarted &&
                        x.LeaseExpiresAt != null && x.LeaseExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionOffboardingOperationStatus.UnknownOutcome)
                .SetProperty(x => x.UpdatedAt, now)
                .SetProperty(x => x.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastSafeErrorCode, safeErrorCode), cancellationToken) == 1;
    }

    public async Task<bool> TryReleaseOffboardingClaimAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long fence,
        DateTimeOffset updatedAt,
        DateTimeOffset nextAttemptAt,
        string safeErrorCode,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.OffboardingOperations
            .Where(x => x.Id == operationId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.ConnectionId == connectionId && x.Fence == fence && x.Status == ConnectionOffboardingOperationStatus.Claimed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionOffboardingOperationStatus.RetryScheduled)
                .SetProperty(x => x.UpdatedAt, updatedAt)
                .SetProperty(x => x.NextAttemptAt, nextAttemptAt)
                .SetProperty(x => x.LeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastSafeErrorCode, safeErrorCode), cancellationToken) == 1;
    }

    public async Task<bool> TryCompleteOffboardingOperationAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long fence,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.OffboardingOperations
            .Where(x => x.Id == operationId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.ConnectionId == connectionId && x.Fence == fence && x.Status == ConnectionOffboardingOperationStatus.ProviderCallStarted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionOffboardingOperationStatus.Completed)
                .SetProperty(x => x.UpdatedAt, completedAt)
                .SetProperty(x => x.LeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastSafeErrorCode, (string?)null), cancellationToken) == 1;
    }

    public async Task<bool> TryRecordOffboardingFailureAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long fence,
        ConnectionOffboardingOperationStatus status,
        DateTimeOffset updatedAt,
        DateTimeOffset? nextAttemptAt,
        string safeErrorCode,
        CancellationToken cancellationToken = default)
    {
        if (status is not (ConnectionOffboardingOperationStatus.RetryScheduled or ConnectionOffboardingOperationStatus.UnknownOutcome or ConnectionOffboardingOperationStatus.TerminalFailure))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.OffboardingOperations
            .Where(x => x.Id == operationId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.ConnectionId == connectionId && x.Fence == fence && x.Status == ConnectionOffboardingOperationStatus.ProviderCallStarted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.UpdatedAt, updatedAt)
                .SetProperty(x => x.NextAttemptAt, nextAttemptAt)
                .SetProperty(x => x.LeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastSafeErrorCode, safeErrorCode), cancellationToken) == 1;
    }

    public async Task<bool> CompleteGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, long fence, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.GenerationCleanups
            .Where(x => x.ConnectionId == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.GenerationId == generationId && x.Fence == fence && x.Status == ConnectionGenerationCleanupStatus.Deleting)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionGenerationCleanupStatus.Deleted)
                .SetProperty(x => x.LeaseExpiresAt, (DateTimeOffset?)null), cancellationToken) == 1;
    }

    public async Task<bool> CancelGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, long fence, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.GenerationCleanups
            .Where(x => x.ConnectionId == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.GenerationId == generationId && x.Fence == fence && x.Status == ConnectionGenerationCleanupStatus.Deleting)
            .ExecuteDeleteAsync(cancellationToken) == 1;
    }

    public async Task<IntegrationConnection?> TryClaimCredentialUpdateAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.Status == ConnectionStatus.Active &&
                        (x.OperationStatus == CredentialOperationStatus.None || x.OperationStatus == CredentialOperationStatus.Completed) &&
                        !db.GenerationCleanups.Any(cleanup => cleanup.ConnectionId == id && cleanup.GenerationId == operationId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Revision, x => x.Revision + 1)
                .SetProperty(x => x.OperationId, operationId)
                .SetProperty(x => x.OperationExpectedRevision, expectedRevision + 1)
                .SetProperty(x => x.OperationFence, x => x.OperationFence + 1)
                .SetProperty(x => x.OperationLeaseExpiresAt, leaseExpiresAt)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Claimed)
                .SetProperty(x => x.OperationSourceGenerationId, x => x.CurrentGenerationId)
                .SetProperty(x => x.PlannedGenerationId, operationId)
                .SetProperty(x => x.PlannedSecretName, ManagedSecretNames.ForGeneration(id, operationId))
                .SetProperty(x => x.StagedSecretName, (string?)null)
                .SetProperty(x => x.StagedGenerationId, (string?)null)
                .SetProperty(x => x.StagedCredentialKind, (ConnectionCredentialKind?)null)
                .SetProperty(x => x.StagedCredentialExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastSafeErrorCode, (string?)null), cancellationToken);
        if (rows != 1)
        {
            return null;
        }

        return await Scoped(db, id, tenantId, environmentId).AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
    }

    public Task<IntegrationConnection?> TryClaimRefreshAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken = default) =>
        TryClaimCredentialUpdateAsync(id, tenantId, environmentId, expectedRevision, operationId, leaseExpiresAt, cancellationToken);

    public async Task<bool> TryAcceptCredentialUpdateAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        long fence,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.OperationExpectedRevision == expectedRevision && x.OperationId == operationId &&
                        x.OperationFence == fence && x.Status == ConnectionStatus.Active && x.OperationStatus == CredentialOperationStatus.Claimed &&
                        x.OperationLeaseExpiresAt != null && x.OperationLeaseExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.OperationStatus, CredentialOperationStatus.CredentialReceived), cancellationToken) == 1;
    }

    public async Task<bool> TryStartProviderCallAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.OperationExpectedRevision == expectedRevision && x.OperationId == operationId &&
                        x.OperationFence == fence && x.Status == ConnectionStatus.Active && x.OperationStatus == CredentialOperationStatus.Claimed &&
                        x.OperationLeaseExpiresAt != null && x.OperationLeaseExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.OperationStatus, CredentialOperationStatus.ProviderCallStarted), cancellationToken) == 1;
    }

    public Task<bool> TryReleaseUnstartedRefreshAsync(
        string id,
        string tenantId,
        string environmentId,
        string operationId,
        long fence,
        string safeErrorCode,
        CancellationToken cancellationToken = default) =>
        ReleaseUnstartedRefreshAsync(id, tenantId, environmentId, operationId, fence, safeErrorCode, null, cancellationToken);

    public Task<bool> TryReleaseExpiredRefreshClaimAsync(
        string id,
        string tenantId,
        string environmentId,
        string operationId,
        long fence,
        DateTimeOffset now,
        string safeErrorCode,
        CancellationToken cancellationToken = default) =>
        ReleaseUnstartedRefreshAsync(id, tenantId, environmentId, operationId, fence, safeErrorCode, now, cancellationToken);

    private async Task<bool> ReleaseUnstartedRefreshAsync(
        string id,
        string tenantId,
        string environmentId,
        string operationId,
        long fence,
        string safeErrorCode,
        DateTimeOffset? expiredAt,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = Scoped(db, id, tenantId, environmentId)
            .Where(x => x.OperationId == operationId && x.OperationFence == fence &&
                        (x.Status == ConnectionStatus.Active || x.Status == ConnectionStatus.Disconnected) &&
                        x.OperationStatus == CredentialOperationStatus.Claimed && x.CurrentGenerationId == x.OperationSourceGenerationId);

        if (expiredAt.HasValue)
        {
            query = query.Where(x => x.Status == ConnectionStatus.Disconnected ||
                                     x.OperationLeaseExpiresAt != null && x.OperationLeaseExpiresAt <= expiredAt.Value);
        }

        return await query.ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Revision, x => x.Revision + 1)
            .SetProperty(x => x.OperationId, (string?)null)
            .SetProperty(x => x.OperationExpectedRevision, 0)
            .SetProperty(x => x.OperationLeaseExpiresAt, (DateTimeOffset?)null)
            .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Completed)
            .SetProperty(x => x.OperationSourceGenerationId, (string?)null)
            .SetProperty(x => x.PlannedSecretName, (string?)null)
            .SetProperty(x => x.PlannedGenerationId, (string?)null)
            .SetProperty(x => x.StagedSecretName, (string?)null)
            .SetProperty(x => x.StagedGenerationId, (string?)null)
            .SetProperty(x => x.StagedCredentialKind, (ConnectionCredentialKind?)null)
            .SetProperty(x => x.StagedCredentialExpiresAt, (DateTimeOffset?)null)
            .SetProperty(x => x.LastSafeErrorCode, safeErrorCode), cancellationToken) == 1;
    }

    public async Task<bool> TryRecordStagedGenerationAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        long fence,
        string secretName,
        string generationId,
        CancellationToken cancellationToken = default) =>
        await TryRecordStagedGenerationAsync(id, tenantId, environmentId, expectedRevision, operationId, fence,
            secretName, generationId, null, null, cancellationToken);

    public async Task<bool> TryRecordStagedGenerationAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        long fence,
        string secretName,
        string generationId,
        ConnectionCredentialKind? credentialKind,
        DateTimeOffset? credentialExpiresAt,
        CancellationToken cancellationToken = default)
    {
        credentialExpiresAt = credentialExpiresAt?.ToUniversalTime();
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            // Staging deliberately ignores current Connection.Revision and Status: disconnect or another state
            // change must not erase the durable fact that the provider may have rotated its one-time token.
            .Where(x => x.OperationExpectedRevision == expectedRevision && x.OperationId == operationId &&
                        x.OperationFence == fence && x.PlannedSecretName == secretName && x.PlannedGenerationId == generationId &&
                        (x.OperationStatus == CredentialOperationStatus.ProviderCallStarted || x.OperationStatus == CredentialOperationStatus.CredentialReceived) &&
                        !db.GenerationCleanups.Any(cleanup => cleanup.ConnectionId == id && cleanup.GenerationId == generationId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.StagedSecretName, secretName)
                .SetProperty(x => x.StagedGenerationId, generationId)
                .SetProperty(x => x.StagedCredentialKind, credentialKind)
                .SetProperty(x => x.StagedCredentialExpiresAt, credentialExpiresAt)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Staged), cancellationToken) == 1;
    }

    public async Task<bool> TryPublishGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.OperationExpectedRevision == expectedRevision && x.OperationId == operationId &&
                        x.OperationFence == fence && x.Status == ConnectionStatus.Active && x.OperationStatus == CredentialOperationStatus.Staged &&
                        x.StagedSecretName != null && x.StagedGenerationId != null &&
                        !db.GenerationCleanups.Any(cleanup => cleanup.ConnectionId == id && cleanup.GenerationId == x.StagedGenerationId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.CurrentSecretName, x => x.StagedSecretName)
                .SetProperty(x => x.CurrentGenerationId, x => x.StagedGenerationId)
                .SetProperty(x => x.CredentialKind, x => x.StagedCredentialKind)
                .SetProperty(x => x.CredentialExpiresAt, x => x.StagedCredentialExpiresAt)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Completed)
                .SetProperty(x => x.OperationLeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken) == 1;
    }

    public Task<bool> TryPromoteRecoveryGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default) =>
        TryPromoteRecoveryGenerationAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, null, null, cancellationToken);

    public async Task<bool> TryPromoteRecoveryGenerationAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        long fence,
        ConnectionCredentialKind? credentialKind,
        DateTimeOffset? credentialExpiresAt,
        CancellationToken cancellationToken = default)
    {
        credentialExpiresAt = credentialExpiresAt?.ToUniversalTime();
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.Status == ConnectionStatus.RecoveryRequired &&
                        x.OperationStatus == CredentialOperationStatus.RecoveryRequired && x.OperationId == operationId &&
                        x.OperationFence == fence && x.OperationExpectedRevision < expectedRevision &&
                        (x.CurrentGenerationId == x.OperationSourceGenerationId ||
                         (x.CurrentGenerationId == null && x.OperationSourceGenerationId == null)) &&
                        x.PlannedSecretName != null && x.PlannedGenerationId != null &&
                        (x.StagedSecretName == null || x.StagedSecretName == x.PlannedSecretName) &&
                        (x.StagedGenerationId == null || x.StagedGenerationId == x.PlannedGenerationId) &&
                        !db.GenerationCleanups.Any(cleanup => cleanup.ConnectionId == id && cleanup.GenerationId == x.PlannedGenerationId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.CurrentSecretName, x => x.PlannedSecretName)
                .SetProperty(x => x.CurrentGenerationId, x => x.PlannedGenerationId)
                .SetProperty(x => x.CredentialKind, credentialKind)
                .SetProperty(x => x.CredentialExpiresAt, credentialExpiresAt)
                .SetProperty(x => x.Status, ConnectionStatus.Active)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Completed)
                .SetProperty(x => x.OperationLeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastSafeErrorCode, (string?)null)
                .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken) == 1;
    }

    public async Task<bool> MarkRecoveryRequiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, string safeErrorCode, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.OperationId == operationId && x.OperationFence == fence &&
                        x.OperationStatus != CredentialOperationStatus.Completed && x.OperationStatus != CredentialOperationStatus.RecoveryRequired)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, x => x.Status == ConnectionStatus.Active ? ConnectionStatus.RecoveryRequired : x.Status)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.RecoveryRequired)
                .SetProperty(x => x.LastSafeErrorCode, safeErrorCode)
                .SetProperty(x => x.OperationLeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.Revision, x => x.Revision + (x.Status == ConnectionStatus.Active ? 1 : 0)), cancellationToken) == 1;
    }

    public async Task<bool> TryMarkRecoveryRequiredIfLeaseExpiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, DateTimeOffset now, string safeErrorCode, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.OperationId == operationId && x.OperationFence == fence &&
                        x.OperationLeaseExpiresAt != null && x.OperationLeaseExpiresAt <= now &&
                        (x.OperationStatus == CredentialOperationStatus.ProviderCallStarted || x.OperationStatus == CredentialOperationStatus.CredentialReceived))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, x => x.Status == ConnectionStatus.Active ? ConnectionStatus.RecoveryRequired : x.Status)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.RecoveryRequired)
                .SetProperty(x => x.LastSafeErrorCode, safeErrorCode)
                .SetProperty(x => x.OperationLeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.Revision, x => x.Revision + (x.Status == ConnectionStatus.Active ? 1 : 0)), cancellationToken) == 1;
    }

    public async Task<bool> TryRestoreSourceGenerationAfterMissingPlanAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        long fence,
        string sourceGenerationId,
        string safeErrorCode,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.Status == ConnectionStatus.RecoveryRequired &&
                        x.OperationStatus == CredentialOperationStatus.RecoveryRequired && x.OperationId == operationId &&
                        x.OperationFence == fence && x.OperationExpectedRevision < expectedRevision &&
                        x.CurrentGenerationId == sourceGenerationId && x.OperationSourceGenerationId == sourceGenerationId &&
                        x.PlannedGenerationId == operationId && x.StagedGenerationId == null &&
                        x.StagedSecretName == null && x.OperationLeaseExpiresAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionStatus.Active)
                .SetProperty(x => x.OperationId, (string?)null)
                .SetProperty(x => x.OperationExpectedRevision, 0)
                .SetProperty(x => x.OperationFence, x => x.OperationFence + 1)
                .SetProperty(x => x.OperationSourceGenerationId, (string?)null)
                .SetProperty(x => x.PlannedSecretName, (string?)null)
                .SetProperty(x => x.PlannedGenerationId, (string?)null)
                .SetProperty(x => x.StagedSecretName, (string?)null)
                .SetProperty(x => x.StagedGenerationId, (string?)null)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Completed)
                .SetProperty(x => x.LastSafeErrorCode, safeErrorCode)
                .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken) == 1;
    }

    public async Task<bool> TryDisconnectAsync(string id, string tenantId, string environmentId, long expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && (x.Status == ConnectionStatus.Active || x.Status == ConnectionStatus.RecoveryRequired))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionStatus.Disconnected)
                .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken) == 1;
    }

    private static IQueryable<IntegrationConnection> Scoped(ConnectionsElsaDbContext db, string id, string tenantId, string environmentId) =>
        db.Connections.Where(x => x.Id == id && x.TenantId == tenantId && x.EnvironmentId == environmentId);
}
