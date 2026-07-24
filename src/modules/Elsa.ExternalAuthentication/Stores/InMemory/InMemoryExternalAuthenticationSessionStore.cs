using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Stores.InMemory;

public sealed class InMemoryExternalAuthenticationSessionStore(ISystemClock clock) : IExternalAuthenticationSessionStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, ExternalAuthenticationSession> _sessions = new(StringComparer.Ordinal);

    public ValueTask<IReadOnlyCollection<ExternalAuthenticationSession>> FindAsync(ExternalAuthenticationSessionFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var sessions = _sessions.Values
                .Where(x => string.Equals(x.TenantId, filter.TenantId, StringComparison.Ordinal))
                .Where(x => filter.UserId is null || string.Equals(x.UserId, filter.UserId, StringComparison.Ordinal))
                .Where(x => filter.ConnectionId is null || string.Equals(x.ConnectionId, filter.ConnectionId, StringComparison.Ordinal))
                .Where(x => filter.Status is null ||
                    filter.Status.Equals("active", StringComparison.OrdinalIgnoreCase) && x.RevokedAt is null ||
                    filter.Status.Equals("revoked", StringComparison.OrdinalIgnoreCase) && x.RevokedAt is not null)
                .Select(Clone)
                .ToArray();
            return ValueTask.FromResult<IReadOnlyCollection<ExternalAuthenticationSession>>(sessions);
        }
    }

    public ValueTask<ExternalAuthenticationSession?> FindByIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
            return ValueTask.FromResult(_sessions.TryGetValue(sessionId, out var session) ? Clone(session) : null);
    }

    public ValueTask<ExternalAuthenticationSession?> FindByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
            return ValueTask.FromResult(_sessions.Values.FirstOrDefault(x => string.Equals(x.CurrentRefreshTokenHash, refreshTokenHash, StringComparison.Ordinal)) is { } session ? Clone(session) : null);
    }

    public ValueTask SaveAsync(ExternalAuthenticationSession session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
            _sessions[session.Id] = Clone(session);

        return ValueTask.CompletedTask;
    }

    public ValueTask<ExternalAuthenticationSessionRotationResult> TryRotateRefreshTokenAsync(string sessionId, string refreshTokenHash, long expectedGeneration, string nextRefreshTokenHash, DateTimeOffset refreshedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return ValueTask.FromResult<ExternalAuthenticationSessionRotationResult>(new ExternalAuthenticationSessionRotationResult.NotFound());

            if (session.RevokedAt != null)
                return ValueTask.FromResult<ExternalAuthenticationSessionRotationResult>(new ExternalAuthenticationSessionRotationResult.Revoked());

            if (session.ExpiresAt <= clock.UtcNow || session.RefreshExpiresAt <= clock.UtcNow)
            {
                session.RevokedAt = clock.UtcNow;
                session.RevocationReason = "expired";
                return ValueTask.FromResult<ExternalAuthenticationSessionRotationResult>(new ExternalAuthenticationSessionRotationResult.Expired());
            }

            if (!string.Equals(session.CurrentRefreshTokenHash, refreshTokenHash, StringComparison.Ordinal) || session.RefreshGeneration != expectedGeneration)
            {
                session.RevokedAt = clock.UtcNow;
                session.RevocationReason = "refresh_token_reuse";
                return ValueTask.FromResult<ExternalAuthenticationSessionRotationResult>(new ExternalAuthenticationSessionRotationResult.Reused());
            }

            session.CurrentRefreshTokenHash = nextRefreshTokenHash;
            session.RefreshGeneration++;
            session.LastRefreshedAt = refreshedAt;
            return ValueTask.FromResult<ExternalAuthenticationSessionRotationResult>(new ExternalAuthenticationSessionRotationResult.Rotated(Clone(session)));
        }
    }

    public ValueTask<bool> RevokeAsync(string sessionId, string reason, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_sessions.TryGetValue(sessionId, out var session) || session.RevokedAt != null)
                return ValueTask.FromResult(false);

            session.RevokedAt = revokedAt;
            session.RevocationReason = reason;
            return ValueTask.FromResult(true);
        }
    }

    private static ExternalAuthenticationSession Clone(ExternalAuthenticationSession session) => new()
    {
        Id = session.Id,
        AuthenticationClientId = session.AuthenticationClientId,
        TenantId = session.TenantId,
        UserId = session.UserId,
        ConnectionId = session.ConnectionId,
        ConnectionMaterialRevision = session.ConnectionMaterialRevision,
        SecretGenerationFingerprint = session.SecretGenerationFingerprint,
        Issuer = session.Issuer,
        SubjectHash = session.SubjectHash,
        ExternalGrants = session.ExternalGrants.ToArray(),
        StartedAt = session.StartedAt,
        LastRefreshedAt = session.LastRefreshedAt,
        ExpiresAt = session.ExpiresAt,
        RefreshExpiresAt = session.RefreshExpiresAt,
        CurrentRefreshTokenHash = session.CurrentRefreshTokenHash,
        RefreshGeneration = session.RefreshGeneration,
        RevokedAt = session.RevokedAt,
        RevocationReason = session.RevocationReason
    };
}
