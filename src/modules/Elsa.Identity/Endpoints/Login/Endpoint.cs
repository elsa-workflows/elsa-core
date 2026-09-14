using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Login;

[PublicAPI]
internal class Login(IUserCredentialsValidator userCredentialsValidator, IAccessTokenIssuer tokenIssuer) : Endpoint<Request, LoginResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/login");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task<LoginResponse> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await userCredentialsValidator.ValidateAsync(request.Username.Trim(), request.Password.Trim(), cancellationToken);

        if (user == null)
            return new LoginResponse(false, null, null);

        var tokens = await tokenIssuer.IssueTokensAsync(user, cancellationToken);

        return new LoginResponse(true, tokens.AccessToken, tokens.RefreshToken);
    }
}