using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Testing.Shared.Authorization;

/// <summary>
/// Authenticates a caller that names itself in a header, holding the permissions named in another header (none when
/// absent); a request without the user header stays anonymous. Lets endpoint tests exercise real authorization.
/// </summary>
public sealed class PermissionHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Header";
    public const string UserHeaderName = "X-Test-User";
    public const string PermissionsHeaderName = "X-Test-Permissions";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(UserHeaderName))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = Request.Headers[PermissionsHeaderName].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => new Claim(PermissionNames.ClaimType, x));
        var identity = new ClaimsIdentity(claims, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
