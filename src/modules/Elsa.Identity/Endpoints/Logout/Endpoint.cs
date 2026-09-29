using System.Security.Claims;
using Elsa.Abstractions;
using Elsa.Identity.Constants;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Elsa.Identity.Endpoints.Logout;

/// <summary>
/// Ends the caller's sign-in session: the refresh token presented, and every refresh token issued in the same session,
/// can no longer be exchanged. Access tokens already issued stay valid until they expire.
/// </summary>
[PublicAPI]
internal class Logout(SessionRevoker sessionRevoker, IOptions<IdentityTokenOptions> identityTokenOptions) : ElsaEndpoint<Request>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/logout");

        // Ownership is checked against the refresh token below, so no grant is required to end your own session.
        RequireAuthenticatedOnly();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var options = identityTokenOptions.Value;

        // An expired refresh token still names its session, and refresh tokens issued after it may be live.
        var validationParameters = options.CreateTokenValidationParameters();
        validationParameters.ValidateLifetime = false;
        validationParameters.LifetimeValidator = null;
        var validationResult = await new JsonWebTokenHandler().ValidateTokenAsync(request.RefreshToken, validationParameters);

        // Not an Elsa Identity refresh token, so there is no session to end here; answer idempotently. Opaque refresh tokens
        // of the External Authentication broker are revoked through the broker's own sign-out.
        if (!validationResult.IsValid)
        {
            await Send.NoContentAsync(cancellationToken);
            return;
        }

        var refreshToken = validationResult.ClaimsIdentity;

        // Anything else would be answered with success while the session it was meant to end stays live.
        if (!string.Equals(refreshToken.FindFirst(TokenUse.ClaimType)?.Value, TokenUse.Refresh, StringComparison.Ordinal))
        {
            AddError(x => x.RefreshToken, "The token is not a refresh token.");
            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }

        if (!IsIssuedToCaller(refreshToken, options.TenantIdClaimsType))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var sessionId = SessionRevoker.GetSessionId(refreshToken, request.RefreshToken);
        var expiresAt = new DateTimeOffset(validationResult.SecurityToken.ValidTo, TimeSpan.Zero);
        await sessionRevoker.RevokeAsync(sessionId, expiresAt, cancellationToken);
        await Send.NoContentAsync(cancellationToken);
    }

    private bool IsIssuedToCaller(ClaimsIdentity refreshToken, string tenantIdClaimType)
    {
        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var userId = refreshToken.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return !string.IsNullOrWhiteSpace(userId)
               && string.Equals(userId, callerId, StringComparison.Ordinal)
               && string.Equals(refreshToken.FindFirst(tenantIdClaimType)?.Value ?? "", User.FindFirst(tenantIdClaimType)?.Value ?? "", StringComparison.Ordinal);
    }
}
