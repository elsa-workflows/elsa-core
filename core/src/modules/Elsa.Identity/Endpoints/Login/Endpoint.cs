using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

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
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await userCredentialsValidator.ValidateAsync(request.Username.Trim(), request.Password.Trim(), cancellationToken);

        if (user == null)
        {
            await Send.ResponseAsync(new LoginResponse(false, null, null), StatusCodes.Status401Unauthorized, cancellationToken);
            return;
        }

        var tokens = await tokenIssuer.IssueTokensAsync(user, cancellationToken);

        await Send.OkAsync(new LoginResponse(true, tokens.AccessToken, tokens.RefreshToken), cancellationToken);
    }
}