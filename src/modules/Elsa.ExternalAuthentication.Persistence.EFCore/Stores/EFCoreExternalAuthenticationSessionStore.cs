using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Microsoft.EntityFrameworkCore;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Stores;

public sealed class EFCoreExternalAuthenticationSessionStore(ExternalAuthenticationDbContextLeaseFactory dbContextFactory, ISystemClock clock) : IExternalAuthenticationSessionStore
{
    public async ValueTask<IReadOnlyCollection<ExternalAuthenticationSession>> FindAsync(ExternalAuthenticationSessionFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var query = lease.DbContext.ExternalAuthenticationSessions.AsNoTracking().Include(x => x.RefreshToken)
            .Where(x => x.TenantId == filter.TenantId);
        if (!string.IsNullOrWhiteSpace(filter.UserId))
            query = query.Where(x => x.UserId == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.ConnectionKey))
            query = query.Where(x => x.ConnectionKey == ConnectionRevisionCalculator.NormalizeKey(filter.ConnectionKey));
        if (string.Equals(filter.Status, "active", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.RevokedAt == null);
        else if (string.Equals(filter.Status, "revoked", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.RevokedAt != null);
        return (await query.ToArrayAsync(cancellationToken)).Select(x => x.ToModel()).ToArray();
    }

    public async ValueTask<ExternalAuthenticationSession?> FindByIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        return (await dbContext.ExternalAuthenticationSessions.AsNoTracking().Include(x => x.RefreshToken).SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken))?.ToModel();
    }

    public async ValueTask<ExternalAuthenticationSession?> FindByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenHash))
            return null;

        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var refreshToken = await dbContext.ExternalAuthenticationRefreshTokens.AsNoTracking().Include(x => x.Session).SingleOrDefaultAsync(x => x.Hash == refreshTokenHash, cancellationToken);
        return refreshToken is null ? null : refreshToken.Session.ToModel(refreshToken.Hash);
    }

    public async ValueTask SaveAsync(ExternalAuthenticationSession session, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var existing = await dbContext.ExternalAuthenticationSessions.Include(x => x.RefreshToken).SingleOrDefaultAsync(x => x.Id == session.Id, cancellationToken);
        if (existing is null)
        {
            var persisted = session.ToPersisted();
            if (session.CurrentRefreshTokenHash is not null)
                persisted.RefreshToken = new PersistedExternalAuthenticationRefreshToken { SessionId = session.Id, Hash = session.CurrentRefreshTokenHash };
            dbContext.ExternalAuthenticationSessions.Add(persisted);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(session.ToPersisted());
            if (session.CurrentRefreshTokenHash is null && existing.RefreshToken is not null)
                dbContext.ExternalAuthenticationRefreshTokens.Remove(existing.RefreshToken);
            else if (session.CurrentRefreshTokenHash is not null && existing.RefreshToken is null)
                dbContext.ExternalAuthenticationRefreshTokens.Add(new PersistedExternalAuthenticationRefreshToken { SessionId = session.Id, Hash = session.CurrentRefreshTokenHash });
            else if (session.CurrentRefreshTokenHash is not null)
                existing.RefreshToken!.Hash = session.CurrentRefreshTokenHash;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<ExternalAuthenticationSessionRotationResult> TryRotateRefreshTokenAsync(string sessionId, string refreshTokenHash, long expectedGeneration, string nextRefreshTokenHash, DateTimeOffset refreshedAt, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var session = await dbContext.ExternalAuthenticationSessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
            return new ExternalAuthenticationSessionRotationResult.NotFound();
        if (session.RevokedAt is not null)
            return new ExternalAuthenticationSessionRotationResult.Revoked();
        if (session.ExpiresAt <= clock.UtcNow || session.RefreshExpiresAt <= clock.UtcNow)
        {
            await dbContext.ExternalAuthenticationSessions.Where(x => x.Id == sessionId && x.RevokedAt == null)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.RevokedAt, clock.UtcNow).SetProperty(y => y.RevocationReason, "expired").SetProperty(y => y.ProtectedUpstreamLogoutHint, (byte[]?)null), cancellationToken);
            return new ExternalAuthenticationSessionRotationResult.Expired();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var rotated = await dbContext.ExternalAuthenticationSessions
            .Where(x => x.Id == sessionId && x.RevokedAt == null && x.RefreshGeneration == expectedGeneration)
            .ExecuteUpdateAsync(x => x
                .SetProperty(y => y.RefreshGeneration, y => y.RefreshGeneration + 1)
                .SetProperty(y => y.LastRefreshedAt, refreshedAt), cancellationToken);
        if (rotated == 1 && await dbContext.ExternalAuthenticationRefreshTokens
                .Where(x => x.SessionId == sessionId && x.Hash == refreshTokenHash)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.Hash, nextRefreshTokenHash), cancellationToken) == 1)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ExternalAuthenticationSessionRotationResult.Rotated((await FindByIdAsync(sessionId, cancellationToken))!);
        }

        await transaction.RollbackAsync(cancellationToken);

        var revoked = await dbContext.ExternalAuthenticationSessions.Where(x => x.Id == sessionId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.RevokedAt, clock.UtcNow).SetProperty(y => y.RevocationReason, "refresh_token_reuse").SetProperty(y => y.ProtectedUpstreamLogoutHint, (byte[]?)null), cancellationToken);
        return revoked == 1 ? new ExternalAuthenticationSessionRotationResult.Reused() : new ExternalAuthenticationSessionRotationResult.Revoked();
    }

    public async ValueTask<bool> RevokeAsync(string sessionId, string reason, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        return await dbContext.ExternalAuthenticationSessions.Where(x => x.Id == sessionId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.RevokedAt, revokedAt).SetProperty(y => y.RevocationReason, reason).SetProperty(y => y.ProtectedUpstreamLogoutHint, (byte[]?)null), cancellationToken) == 1;
    }

    public async ValueTask<int> RevokeActiveForConnectionAsync(string connectionKey, string reason, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionKey);
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        return await lease.DbContext.ExternalAuthenticationSessions
            .Where(x => x.ConnectionKey == ConnectionRevisionCalculator.NormalizeKey(connectionKey) && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.RevokedAt, revokedAt).SetProperty(y => y.RevocationReason, reason).SetProperty(y => y.ProtectedUpstreamLogoutHint, (byte[]?)null), cancellationToken);
    }
}
