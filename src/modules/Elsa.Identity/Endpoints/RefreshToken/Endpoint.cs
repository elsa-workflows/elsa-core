using Elsa.Extensions;
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
    }

    /// <inheritdoc />
    public override async Task<LoginResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var user = await _userProvider.FindByNameAsync(User.Identity!.Name!, cancellationToken);

        if (user == null)
            return new LoginResponse(false, null, null);

        var tokens = await _tokenIssuer.IssueTokensAsync(user, cancellationToken);

        return new LoginResponse(true, tokens.AccessToken, tokens.RefreshToken);
    }
}