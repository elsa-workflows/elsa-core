using System.Security.Claims;
using System.Text.Json;
using Elsa;
using Elsa.Authorization;
using Elsa.Common;
using Elsa.Identity.Constants;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

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

    [Fact(DisplayName = "A zero-grant token carries the permissions sentinel, not an omitted claim")]
    public async Task ZeroGrantsEmitTheEmptySetSentinel()
    {
        var result = await CreateService().IssueAccessTokenAsync(EmptyGrantContext());
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(result.Token);
        var payload = StudioWasmJwtParser.ReadPayload(result.Token);

        Assert.Contains(jwt.Claims, x => x.Type == PermissionNames.ClaimType && x.Value == PermissionNames.None);
        Assert.Single(jwt.Claims, x => x.Type == PermissionNames.ClaimType);
        // A JSON array or empty string is dropped by JwtBearer and Studio's WASM parser. A non-empty
        // string that is not a permission survives both and is not a grant.
        Assert.Equal(JsonValueKind.String, payload.GetProperty(PermissionNames.ClaimType).ValueKind);
        Assert.Equal(PermissionNames.None, payload.GetProperty(PermissionNames.ClaimType).GetString());
    }

    [Fact(DisplayName = "A refresh token with zero grants carries the same sentinel as the access token")]
    public async Task ZeroGrantRefreshTokensCarryTheSentinel()
    {
        var result = await CreateService().IssueRefreshTokenAsync(EmptyGrantContext());
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(result.Token);

        Assert.Contains(jwt.Claims, x => x.Type == PermissionNames.ClaimType && x.Value == PermissionNames.None);
    }

    [Fact(DisplayName = "The empty-set sentinel survives JwtBearer validation and Studio WASM parsing as a known empty set")]
    public async Task EmptySetSentinelRoundTripsToAKnownEmptySet()
    {
        var options = CreateOptions();
        var result = await new DefaultElsaTokenService(new TestSystemClock(DateTimeOffset.UtcNow), options).IssueAccessTokenAsync(EmptyGrantContext());

        var jwtBearerClaims = await ValidateWithJwtBearerAsync(result.Token, options.Value);
        var wasmClaims = StudioWasmJwtParser.Parse(result.Token);

        AssertKnownEmpty(jwtBearerClaims);
        AssertKnownEmpty(wasmClaims);
        Assert.Empty(new PermissionEvaluator().GetGrants(new ClaimsPrincipal(new ClaimsIdentity(jwtBearerClaims, "jwt"))));
    }

    [Fact(DisplayName = "A token that omits the permissions claim stays without grants after JwtBearer and WASM parsing")]
    public async Task AMissingPermissionsClaimStaysUnknown()
    {
        var options = CreateOptions().Value;
        var token = CreateTokenWithoutPermissionsClaim(options);

        var jwtBearerClaims = await ValidateWithJwtBearerAsync(token, options);
        var wasmClaims = StudioWasmJwtParser.Parse(token);

        Assert.DoesNotContain(jwtBearerClaims, x => x.Type == PermissionNames.ClaimType);
        Assert.DoesNotContain(wasmClaims, x => x.Type == PermissionNames.ClaimType);
        // Studio's ClaimsPermissionService maps "no permissions claims" to UserPermissions.Unknown.
        Assert.False(HasPermissionsClaim(jwtBearerClaims));
        Assert.False(HasPermissionsClaim(wasmClaims));
    }

    [Fact(DisplayName = "An empty JSON array permissions claim is unknown after Studio WASM parsing")]
    public async Task EmptyJsonArrayIsUnknownAfterStudioWasmParsing()
    {
        var options = CreateOptions().Value;
        var token = CreateTokenWithPermissionsPayload(options, Array.Empty<string>());
        var payload = StudioWasmJwtParser.ReadPayload(token);

        // JwtBearer may keep a claim for `[]`, but Studio's WASM parser expands arrays item-by-item
        // and therefore produces zero permissions claims — UserPermissions.Unknown.
        Assert.Equal(JsonValueKind.Array, payload.GetProperty(PermissionNames.ClaimType).ValueKind);
        Assert.Equal(0, payload.GetProperty(PermissionNames.ClaimType).GetArrayLength());
        Assert.False(HasPermissionsClaim(StudioWasmJwtParser.Parse(token)));
    }

    [Fact(DisplayName = "A refresh token starts a new session unless the context continues one")]
    public async Task IssueRefreshTokenCarriesTheSession()
    {
        var service = CreateService();
        var context = new TokenIssuanceContext(new User { Id = "user-1", Name = "alice" }, [], [], []);

        var first = ReadSessionId(await service.IssueRefreshTokenAsync(context));
        var second = ReadSessionId(await service.IssueRefreshTokenAsync(context));
        var continued = ReadSessionId(await service.IssueRefreshTokenAsync(context with { Session = new(first!, DateTimeOffset.UtcNow) }));

        Assert.False(string.IsNullOrWhiteSpace(first));
        Assert.NotEqual(first, second);
        Assert.Equal(first, continued);
    }

    [Fact(DisplayName = "An access token carries no session, because revoking one does not revoke access tokens")]
    public async Task IssueAccessTokenCarriesNoSession()
    {
        var context = new TokenIssuanceContext(new User { Id = "user-1", Name = "alice" }, [], [], []) { Session = new("session-1", DateTimeOffset.UtcNow) };

        var token = await CreateService().IssueAccessTokenAsync(context);

        Assert.DoesNotContain(new JsonWebTokenHandler().ReadJsonWebToken(token.Token).Claims, x => x.Type == CustomClaimTypes.SessionId);
    }

    [Fact(DisplayName = "A refresh token carries the latest expiry of its session forward, and its own when that is later")]
    public async Task IssueRefreshTokenCarriesTheSessionExpiryForward()
    {
        var service = CreateService();
        var context = new TokenIssuanceContext(new User { Id = "user-1", Name = "alice" }, [], [], []);
        var longLived = new SignInSession("session-1", DateTimeOffset.UtcNow.AddDays(30));
        var shortLived = longLived with { ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) };

        var started = await service.IssueRefreshTokenAsync(context);
        var continuedLongLived = await service.IssueRefreshTokenAsync(context with { Session = longLived });
        var continuedShortLived = await service.IssueRefreshTokenAsync(context with { Session = shortLived });

        Assert.Equal(started.ExpiresAt.ToUnixTimeSeconds(), ReadSessionExpiresAt(started));
        Assert.Equal(longLived.ExpiresAt.ToUnixTimeSeconds(), ReadSessionExpiresAt(continuedLongLived));
        Assert.Equal(continuedShortLived.ExpiresAt.ToUnixTimeSeconds(), ReadSessionExpiresAt(continuedShortLived));
    }

    private static DefaultElsaTokenService CreateService() =>
        new(new TestSystemClock(DateTimeOffset.UtcNow), CreateOptions());

    private static IOptions<IdentityTokenOptions> CreateOptions() =>
        Microsoft.Extensions.Options.Options.Create(new IdentityTokenOptions { SigningKey = IdentityTokenTestConstants.SigningKey });

    private static TokenIssuanceContext EmptyGrantContext() =>
        new(new User { Id = "user-1", Name = "alice" }, [], [], []);

    private static string? ReadSessionId(IssuedAccessToken token) =>
        new JsonWebTokenHandler().ReadJsonWebToken(token.Token).Claims.FirstOrDefault(x => x.Type == CustomClaimTypes.SessionId)?.Value;

    private static long ReadSessionExpiresAt(IssuedAccessToken token) =>
        long.Parse(new JsonWebTokenHandler().ReadJsonWebToken(token.Token).GetClaim(CustomClaimTypes.SessionExpiresAt).Value);

    private static async Task<IReadOnlyCollection<Claim>> ValidateWithJwtBearerAsync(string token, IdentityTokenOptions options)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, options.CreateTokenValidationParameters());
        Assert.True(result.IsValid, result.Exception?.Message);
        return result.ClaimsIdentity.Claims.ToArray();
    }

    private static bool HasPermissionsClaim(IEnumerable<Claim> claims) =>
        claims.Any(x => x.Type == PermissionNames.ClaimType);

    private static void AssertKnownEmpty(IEnumerable<Claim> claims)
    {
        var permissionClaims = claims.Where(x => x.Type == PermissionNames.ClaimType).ToList();
        Assert.NotEmpty(permissionClaims);
        Assert.All(permissionClaims, claim => Assert.False(Permission.TryParse(claim.Value, out _)));
    }

    private static string CreateTokenWithoutPermissionsClaim(IdentityTokenOptions options) =>
        CreateTokenWithPermissionsPayload(options, permissions: null);

    private static string CreateTokenWithPermissionsPayload(IdentityTokenOptions options, object? permissions)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = "user-1",
            [JwtRegisteredClaimNames.Name] = "alice",
            [TokenUse.ClaimType] = TokenUse.Access
        };

        if (permissions is not null)
            claims[PermissionNames.ClaimType] = permissions;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningCredentials = new SigningCredentials(options.CreateSecurityKey(), SecurityAlgorithms.HmacSha256Signature)
        });
    }

    private sealed class TestSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
