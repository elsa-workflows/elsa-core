using System.Security.Claims;
using Elsa.Extensions;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
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
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var userId = RefreshTokenSubject.FindUserId(User);

        if (userId is null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        var user = await _userProvider.FindByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        // The refresh-token scheme rejects revoked sessions and puts the session on the principal; the new refresh
        // token continues it, so revoking the session later also revokes this one.
        var session = SessionRevoker.FindSession((ClaimsIdentity)User.Identity!);
        var tokens = await _tokenIssuer.IssueTokensAsync(user, session, cancellationToken);

        await Send.OkAsync(new LoginResponse(true, tokens.AccessToken, tokens.RefreshToken), cancellationToken);
    }
}
