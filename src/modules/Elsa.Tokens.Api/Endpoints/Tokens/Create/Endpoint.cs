using Elsa.Api.Common.Services;
using FastEndpoints;

namespace Elsa.Tokens.Api.Endpoints.Tokens.Create;

public class Create : Endpoint<Request, Response>
{
    private readonly ICredentialsValidator _credentialsValidator;
    private readonly IAccessTokenIssuer _tokenIssuer;

    public Create(ICredentialsValidator credentialsValidator, IAccessTokenIssuer tokenIssuer)
    {
        _credentialsValidator = credentialsValidator;
        _tokenIssuer = tokenIssuer;
    }

    public override void Configure()
    {
        Post("tokens");
        AllowAnonymous();
    }

    public override async Task<Response> ExecuteAsync(Request req, CancellationToken ct)
    {
        var payload = req.Credentials;
        var validationResult = await _credentialsValidator.ValidateCredentialsAsync(payload, ct);

        if (!validationResult.IsValid)
        {
            await SendUnauthorizedAsync(ct);
            return null!;
        }

        var token = await _tokenIssuer.CreateTokenAsync(payload, validationResult.Context, ct);

        return new Response
        {
            AccessToken = token
        };
    }
}