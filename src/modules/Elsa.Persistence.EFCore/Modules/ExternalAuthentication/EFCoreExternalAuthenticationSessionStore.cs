using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.Persistence.EFCore.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.ExternalAuthentication;

public sealed class EFCoreExternalAuthenticationSessionStore(ExternalAuthenticationDbContextFactory dbContextFactory, ISystemClock clock) : IExternalAuthenticationSessionStore
{
    public async ValueTask<IReadOnlyCollection<ExternalAuthenticationSession>> FindAsync(ExternalAuthenticationSessionFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var query = lease.DbContext.ExternalAuthenticationSessions.AsNoTracking()
            .Where(x => x.TenantId == filter.TenantId);
        if (!string.IsNullOrWhiteSpace(filter.UserId))
            query = query.Where(x => x.UserId == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.ConnectionId))
            query = query.Where(x => x.ConnectionId == filter.ConnectionId);
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
        return (await dbContext.ExternalAuthenticationSessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken))?.ToModel();
    }

    public async ValueTask<ExternalAuthenticationSession?> FindByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        return (await dbContext.ExternalAuthenticationSessions.AsNoTracking().SingleOrDefaultAsync(x => x.CurrentRefreshTokenHash == refreshTokenHash, cancellationToken))?.ToModel();
    }

    public async ValueTask SaveAsync(ExternalAuthenticationSession session, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var existing = await dbContext.ExternalAuthenticationSessions.SingleOrDefaultAsync(x => x.Id == session.Id, cancellationToken);
        if (existing is null)
            dbContext.ExternalAuthenticationSessions.Add(session.ToPersisted());
        else
            dbContext.Entry(existing).CurrentValues.SetValues(session.ToPersisted());
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
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.RevokedAt, clock.UtcNow).SetProperty(y => y.RevocationReason, "expired"), cancellationToken);
            return new ExternalAuthenticationSessionRotationResult.Expired();
        }

        var rotated = await dbContext.ExternalAuthenticationSessions
            .Where(x => x.Id == sessionId && x.RevokedAt == null && x.CurrentRefreshTokenHash == refreshTokenHash && x.RefreshGeneration == expectedGeneration)
            .ExecuteUpdateAsync(x => x
                .SetProperty(y => y.CurrentRefreshTokenHash, nextRefreshTokenHash)
                .SetProperty(y => y.RefreshGeneration, y => y.RefreshGeneration + 1)
                .SetProperty(y => y.LastRefreshedAt, refreshedAt), cancellationToken);
        if (rotated == 1)
            return new ExternalAuthenticationSessionRotationResult.Rotated((await FindByIdAsync(sessionId, cancellationToken))!);

        var revoked = await dbContext.ExternalAuthenticationSessions.Where(x => x.Id == sessionId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.RevokedAt, clock.UtcNow).SetProperty(y => y.RevocationReason, "refresh_token_reuse"), cancellationToken);
        return revoked == 1 ? new ExternalAuthenticationSessionRotationResult.Reused() : new ExternalAuthenticationSessionRotationResult.Revoked();
    }

    public async ValueTask<bool> RevokeAsync(string sessionId, string reason, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        return await dbContext.ExternalAuthenticationSessions.Where(x => x.Id == sessionId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.RevokedAt, revokedAt).SetProperty(y => y.RevocationReason, reason), cancellationToken) == 1;
    }
}
