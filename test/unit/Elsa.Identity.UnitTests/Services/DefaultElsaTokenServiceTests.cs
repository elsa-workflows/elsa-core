using System.Security.Claims;
using Elsa.Common;
using Elsa.Identity.Constants;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultElsaTokenServiceTests
{
    [Test]
    [DisplayName("Token issuance context is projected into an Elsa access token")]
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

        await Assert.That(result.ExpiresAt).IsEqualTo(clock.UtcNow.AddMinutes(15));
        await Assert.That(token.Claims).Contains(x => x.Type == JwtRegisteredClaimNames.Sub && x.Value == user.Id);
        await Assert.That(token.Claims).Contains(x => x.Type == JwtRegisteredClaimNames.Name && x.Value == user.Name);
        await Assert.That(token.Claims).Contains(x => x.Type == options.Value.TenantIdClaimsType && x.Value == user.TenantId);
        await Assert.That(token.Claims).Contains(x => x.Type == ClaimTypes.Role && x.Value == "operator");
        await Assert.That(token.Claims).Contains(x => x.Type == "permissions" && x.Value == "workflows:read");
        await Assert.That(token.Claims).Contains(x => x.Type == "department" && x.Value == "claims");
        await Assert.That(token.Claims).Contains(x => x.Type == CustomClaimTypes.ExternalAuthenticationSessionId && x.Value == "session-1");
        await Assert.That(token.Claims).Contains(x => x.Type == TokenUse.ClaimType && x.Value == TokenUse.Access);
    }

    private sealed class TestSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}