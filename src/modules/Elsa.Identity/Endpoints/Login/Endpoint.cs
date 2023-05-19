using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using FastEndpoints;

namespace Elsa.Identity.Endpoints.Login;

public class Login : Endpoint<Request, LoginResponse>
{
    private readonly IUserCredentialsValidator _userCredentialsValidator;
    private readonly IAccessTokenIssuer _tokenIssuer;

    public Login(IUserCredentialsValidator userCredentialsValidator, IAccessTokenIssuer tokenIssuer)
    {
        _userCredentialsValidator = userCredentialsValidator;
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
        var user = await _userCredentialsValidator.ValidateAsync(request.Username?.Trim(), request.Password?.Trim(), cancellationToken);

        if (user == null)
            return new LoginResponse(false, null, null);

        var tokens = await _tokenIssuer.IssueTokensAsync(user, cancellationToken);

        return new LoginResponse(true, tokens.AccessToken, tokens.RefreshToken);
    }
}