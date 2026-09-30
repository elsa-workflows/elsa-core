using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Resilience.IntegrationTests;

/// <summary>
/// Authenticates a caller holding the permissions named in a header; a request without the header stays anonymous.
/// </summary>
internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "Test";
    public const string IdentityHeader = "X-Test-Identity";
    public const string PermissionHeader = "X-Test-Permissions";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(PermissionHeader, out var permissionHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = Request.Headers.TryGetValue(IdentityHeader, out var identityHeader)
            ? identityHeader.FirstOrDefault()
            : null;
        var claims = permissionHeader
            .SelectMany(x => x?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
            .Select(x => new Claim("permissions", x))
            .ToList();

        claims.Add(new Claim(ClaimTypes.NameIdentifier, identity ?? "test-user"));

        var claimsIdentity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(claimsIdentity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
