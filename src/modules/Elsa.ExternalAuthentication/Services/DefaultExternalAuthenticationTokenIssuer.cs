using System.Security.Cryptography;
using System.Text;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;

namespace Elsa.ExternalAuthentication.Services;

public sealed class DefaultExternalAuthenticationTokenIssuer(
    IExternalAuthenticationSessionStore sessionStore,
    IIdentityProviderConnectionRegistry connectionRegistry,
    IEnumerable<ISecretBindingResolver> secretBindingResolvers,
    IUserProvider userProvider,
    IRoleProvider roleProvider,
    IElsaTokenService tokenService,
    ISystemClock clock) : IExternalAuthenticationTokenIssuer
{
    public async ValueTask<ExternalTokenResponse> IssueAsync(ExternalAuthenticationSession session, CancellationToken cancellationToken = default)
    {
        var refreshToken = CreateRefreshToken(session.Id);
        session.CurrentRefreshTokenHash = Hash(refreshToken);
        await sessionStore.SaveAsync(session, cancellationToken);
        return await IssueResponseAsync(session, refreshToken, cancellationToken);
    }

    public async ValueTask<ExternalTokenResponse> RefreshAsync(string clientId, SensitiveString refreshToken, CancellationToken cancellationToken = default)
    {
        var rawToken = refreshToken.Reveal();
        var separator = rawToken.IndexOf('.', StringComparison.Ordinal);
        if (separator <= 0 || separator == rawToken.Length - 1)
            throw new InvalidOperationException("The external refresh token is invalid.");
        var sessionId = rawToken[..separator];
        var currentHash = Hash(rawToken);
        var session = await sessionStore.FindByIdAsync(sessionId, cancellationToken);
        if (session is null || !string.Equals(session.AuthenticationClientId, clientId, StringComparison.Ordinal))
            throw new InvalidOperationException("The external refresh token is invalid.");
        var connection = await connectionRegistry.FindByIdAsync(session.TenantId, session.ConnectionId, cancellationToken);
        if (session.RevokedAt != null || session.ExpiresAt <= clock.UtcNow || connection is null || connection.IsShadowed || !connection.Connection.IsEnabled || connection.Connection.ArchivedAt is not null || !string.Equals(connection.Connection.MaterialRevision, session.ConnectionMaterialRevision, StringComparison.Ordinal))
            throw new InvalidOperationException("The external authentication session is no longer valid.");
        if (!string.Equals(session.SecretGenerationFingerprint, await GetSecretFingerprintAsync(connection.Connection.SecretBindings, cancellationToken), StringComparison.Ordinal))
            throw new InvalidOperationException("The external authentication session secrets changed.");

        var nextToken = CreateRefreshToken(session.Id);
        var rotation = await sessionStore.TryRotateRefreshTokenAsync(session.Id, currentHash, session.RefreshGeneration, Hash(nextToken), clock.UtcNow, cancellationToken);
        if (rotation is not ExternalAuthenticationSessionRotationResult.Rotated { Session: var rotated })
            throw new InvalidOperationException("The external refresh token cannot be used.");

        return await IssueResponseAsync(rotated, nextToken, cancellationToken);
    }

    private async ValueTask<ExternalTokenResponse> IssueResponseAsync(ExternalAuthenticationSession session, string refreshToken, CancellationToken cancellationToken)
    {
        var user = await userProvider.FindAsync(new UserFilter { Id = session.UserId }, cancellationToken)
            ?? throw new InvalidOperationException("The external authentication session user no longer exists.");
        var roles = (await roleProvider.FindByIdsAsync(user.Roles, cancellationToken)).ToArray();
        var permissions = roles.SelectMany(x => x.Permissions).Concat(session.ExternalGrants.Select(x => x.Permission)).Distinct(StringComparer.Ordinal).ToArray();
        var accessToken = await tokenService.IssueAccessTokenAsync(new TokenIssuanceContext(user, roles.Select(x => x.Name).ToArray(), permissions, [], session.Id), cancellationToken);
        var now = clock.UtcNow;
        return new ExternalTokenResponse(
            accessToken.Token,
            "Bearer",
            Math.Max(0, (long)(accessToken.ExpiresAt - now).TotalSeconds),
            refreshToken,
            Math.Max(0, (long)(session.RefreshExpiresAt - now).TotalSeconds),
            Math.Max(0, (long)(session.ExpiresAt - now).TotalSeconds));
    }

    private static string CreateRefreshToken(string sessionId) => $"{sessionId}.{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async ValueTask<string> GetSecretFingerprintAsync(IDictionary<string, SecretBinding> bindings, CancellationToken cancellationToken)
    {
        var fingerprints = new List<string>();
        foreach (var (name, binding) in bindings)
        {
            var resolver = secretBindingResolvers.FirstOrDefault(x => string.Equals(x.Type, binding.ResolverType, StringComparison.Ordinal))
                ?? throw new InvalidOperationException("A required secret binding resolver is unavailable.");
            var resolved = await resolver.ResolveAsync(binding, cancellationToken);
            fingerprints.Add($"{name}:{resolved.GenerationFingerprint}");
            resolved.Value.Dispose();
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", fingerprints.OrderBy(x => x, StringComparer.Ordinal)))));
    }
}
