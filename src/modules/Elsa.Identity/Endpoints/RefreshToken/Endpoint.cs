using System.Security.Claims;
using Elsa.Extensions;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.IdentityModel.JsonWebTokens;

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
        var user = await FindUserAsync(cancellationToken);

        if (user == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        var tokens = await _tokenIssuer.IssueTokensAsync(user, cancellationToken);

        await Send.OkAsync(new LoginResponse(true, tokens.AccessToken, tokens.RefreshToken), cancellationToken);
    }

    /// <summary>
    /// Resolves the caller by the token's subject (user id). Name is used only for tokens issued before
    /// <c>sub</c> was present. A present <c>sub</c> that matches no user is rejected, even if a user with
    /// the same name exists.
    /// </summary>
    private Task<User?> FindUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return _userProvider.FindByIdAsync(userId, cancellationToken);
        }

        var userName = User.Identity?.Name
            ?? User.FindFirst(JwtRegisteredClaimNames.Name)?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(userName))
        {
            return Task.FromResult<User?>(null);
        }

        return _userProvider.FindByNameAsync(userName, cancellationToken);
    }
}
