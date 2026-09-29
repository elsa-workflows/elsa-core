using Elsa.Extensions;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.RefreshToken;

/// <summary>
/// Generates a new token for the current user.
/// </summary>
[PublicAPI]
internal class RefreshToken : EndpointWithoutRequest<LoginResponse>
{
    private readonly IUserProvider _userProvider;
    private readonly IAccessTokenIssuer _tokenIssuer;

    /// <inheritdoc />
    public RefreshToken(IUserProvider userProvider, IAccessTokenIssuer tokenIssuer)
    {
        _userProvider = userProvider;
        _tokenIssuer = tokenIssuer;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/refresh-token");
        AuthSchemes(IdentityAuthenticationSchemes.RefreshToken);
    }

    /// <inheritdoc />
    public override async Task<LoginResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var user = await _userProvider.FindByNameAsync(User.Identity!.Name!, cancellationToken);

        if (user == null)
            return new LoginResponse(false, null, null);

        // The refresh-token scheme rejects revoked sessions and puts the session on the principal; the new refresh
        // token continues it, so revoking the session later also revokes this one.
        var sessionId = User.FindFirst(CustomClaimTypes.SessionId)?.Value;
        var tokens = await _tokenIssuer.IssueTokensAsync(user, sessionId, cancellationToken);

        return new LoginResponse(true, tokens.AccessToken, tokens.RefreshToken);
    }
}
