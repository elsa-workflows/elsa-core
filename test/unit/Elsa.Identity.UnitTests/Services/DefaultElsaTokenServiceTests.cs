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
        Assert.Contains(token.Claims, x => x.Type == JwtRegisteredClaimNames.Name && x.Value == user.Name);
        Assert.Contains(token.Claims, x => x.Type == options.Value.TenantIdClaimsType && x.Value == user.TenantId);
        Assert.Contains(token.Claims, x => x.Type == ClaimTypes.Role && x.Value == "operator");
        Assert.Contains(token.Claims, x => x.Type == "permissions" && x.Value == "workflows:read");
        Assert.Contains(token.Claims, x => x.Type == "department" && x.Value == "claims");
        Assert.Contains(token.Claims, x => x.Type == CustomClaimTypes.ExternalAuthenticationSessionId && x.Value == "session-1");
        Assert.Contains(token.Claims, x => x.Type == TokenUse.ClaimType && x.Value == TokenUse.Access);
    }

    private sealed class TestSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
