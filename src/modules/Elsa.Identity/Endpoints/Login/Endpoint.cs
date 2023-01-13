using Elsa.Identity.Models;
using Elsa.Identity.Services;
using FastEndpoints;

namespace Elsa.Identity.Endpoints.Login;

public class Login : Endpoint<Request, LoginResponse>
{
    private readonly ICredentialsValidator _credentialsValidator;
    private readonly IAccessTokenIssuer _tokenIssuer;

    public Login(ICredentialsValidator credentialsValidator, IAccessTokenIssuer tokenIssuer)
    {
        _credentialsValidator = credentialsValidator;
        _tokenIssuer = tokenIssuer;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/login");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task<LoginResponse> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await _credentialsValidator.ValidateAsync(request.Username.Trim(), request.Password.Trim(), cancellationToken);

        if (user == null)
            return new LoginResponse(false, null, null);

        var tokens = await _tokenIssuer.IssueTokensAsync(user, cancellationToken);

        return new LoginResponse(true, tokens.AccessToken, tokens.RefreshToken);
    }
}