using Elsa.Identity.Models;
using Elsa.Identity.Services;
using FastEndpoints;

namespace Elsa.Identity.Endpoints.RefreshToken;

public class RefreshToken : EndpointWithoutRequest<LoginResponse>
{
    private readonly IUserStore _userStore;
    private readonly IAccessTokenIssuer _tokenIssuer;

    /// <inheritdoc />
    public RefreshToken(IUserStore userStore, IAccessTokenIssuer tokenIssuer)
    {
        _userStore = userStore;
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
        var user = await _userStore.FindAsync(User.Identity!.Name!, cancellationToken);

        if (user == null)
            return new LoginResponse(false, null, null);

        var tokens = await _tokenIssuer.IssueTokensAsync(user, cancellationToken);

        return new LoginResponse(true, tokens.AccessToken, tokens.RefreshToken);
    }
}