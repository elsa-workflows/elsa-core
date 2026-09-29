using System.Security.Claims;
using Elsa.Common;
using Elsa.Identity.Constants;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultElsaTokenServiceTests
{
    [Fact(DisplayName = "Token issuance context is projected into an Elsa access token")]
    public async Task IssueAccessTokenProjectsContext()
    {
        var clock = new TestSystemClock(new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero));
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityTokenOptions
        {
            SigningKey = "external-authentication-test-signing-key",
            Issuer = "https://elsa.test",
            Audience = "elsa-api",
            AccessTokenLifetime = TimeSpan.FromMinutes(15)
        });
        var service = new DefaultElsaTokenService(clock, options);
        var user = new User { Id = "user-1", Name = "alice", TenantId = "tenant-a" };
        var context = new TokenIssuanceContext(
            user,
            ["operator"],
            ["workflows:read"],
            [new Claim("department", "claims")],
            "session-1");

        var result = await service.IssueAccessTokenAsync(context);
        var token = new JsonWebTokenHandler().ReadJsonWebToken(result.Token);

        Assert.Equal(clock.UtcNow.AddMinutes(15), result.ExpiresAt);
        Assert.Contains(token.Claims, x => x.Type == JwtRegisteredClaimNames.Sub && x.Value == user.Id);
        Assert.Contains(token.Claims, x => x.Type == JwtRegisteredClaimNames.Name && x.Value == user.Name);
        Assert.Contains(token.Claims, x => x.Type == options.Value.TenantIdClaimsType && x.Value == user.TenantId);
        Assert.Contains(token.Claims, x => x.Type == ClaimTypes.Role && x.Value == "operator");
        Assert.Contains(token.Claims, x => x.Type == "permissions" && x.Value == "workflows:read");
        Assert.Contains(token.Claims, x => x.Type == "department" && x.Value == "claims");
        Assert.Contains(token.Claims, x => x.Type == CustomClaimTypes.ExternalAuthenticationSessionId && x.Value == "session-1");
        Assert.Contains(token.Claims, x => x.Type == TokenUse.ClaimType && x.Value == TokenUse.Access);
    }

    [Fact(DisplayName = "A refresh token starts a new session unless the context continues one")]
    public async Task IssueRefreshTokenCarriesTheSession()
    {
        var service = CreateService();
        var context = new TokenIssuanceContext(new User { Id = "user-1", Name = "alice" }, [], [], []);

        var first = ReadSessionId(await service.IssueRefreshTokenAsync(context));
        var second = ReadSessionId(await service.IssueRefreshTokenAsync(context));
        var continued = ReadSessionId(await service.IssueRefreshTokenAsync(context with { SessionId = first }));

        Assert.False(string.IsNullOrWhiteSpace(first));
        Assert.NotEqual(first, second);
        Assert.Equal(first, continued);
    }

    [Fact(DisplayName = "An access token carries no session, because revoking one does not revoke access tokens")]
    public async Task IssueAccessTokenCarriesNoSession()
    {
        var context = new TokenIssuanceContext(new User { Id = "user-1", Name = "alice" }, [], [], []) { SessionId = "session-1" };

        var token = await CreateService().IssueAccessTokenAsync(context);

        Assert.DoesNotContain(new JsonWebTokenHandler().ReadJsonWebToken(token.Token).Claims, x => x.Type == CustomClaimTypes.SessionId);
    }

    private static DefaultElsaTokenService CreateService() =>
        new(new TestSystemClock(DateTimeOffset.UtcNow), Microsoft.Extensions.Options.Options.Create(new IdentityTokenOptions { SigningKey = IdentityTokenTestConstants.SigningKey }));

    private static string? ReadSessionId(IssuedAccessToken token) =>
        new JsonWebTokenHandler().ReadJsonWebToken(token.Token).Claims.FirstOrDefault(x => x.Type == CustomClaimTypes.SessionId)?.Value;

    private sealed class TestSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
