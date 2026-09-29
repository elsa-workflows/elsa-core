using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Elsa.Common;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Features;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Endpoints;

/// <summary>
/// Signs in, refreshes and signs out through the real endpoints, authentication schemes, token services and the
/// in-memory revocation store.
/// </summary>
public sealed class LogoutEndpointTests : IAsyncLifetime
{
    private static readonly User Alice = new() { Id = "alice-id", Name = "alice" };
    private static readonly User Bob = new() { Id = "bob-id", Name = "bob" };
    private readonly MutableSystemClock _clock = new();
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private IdentityTokenOptions _tokenOptions = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        var module = Substitute.For<IModule>();
        module.Services.Returns(builder.Services);
        new IdentityFeature(module) { TokenOptions = options => options.SigningKey = IdentityTokenTestConstants.SigningKey }.Apply();
        new DefaultAuthenticationFeature(module).Apply();

        // The startup diagnostics need the permission catalog, which this host does not install.
        foreach (var diagnostic in builder.Services.Where(x => x.ImplementationType?.Namespace == typeof(StoredPermissionValidator).Namespace).ToList())
            builder.Services.Remove(diagnostic);

        builder.Services
            .AddSingleton<ISystemClock>(_clock)
            .AddScoped<IUserCredentialsValidator, NameOnlyCredentialsValidator>()
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(IdentityFeature).Assembly];
                options.Filter = x => x.Namespace is "Elsa.Identity.Endpoints.Login" or "Elsa.Identity.Endpoints.RefreshToken" or "Elsa.Identity.Endpoints.Logout";
            });

        _app = builder.Build();
        _tokenOptions = _app.Services.GetRequiredService<IOptions<IdentityTokenOptions>>().Value;
        var users = _app.Services.GetRequiredService<MemoryStore<User>>();
        users.Save(Alice, x => x.Id);
        users.Save(Bob, x => x.Id);
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task RevokedRefreshTokenIsRejectedLikeAnInvalidOne()
    {
        var tokens = await LoginAsync(Alice);
        await AssertRefreshWorksAsync(tokens.RefreshToken);

        await AssertLogoutAsync(HttpStatusCode.NoContent, tokens.AccessToken, tokens.RefreshToken);

        await AssertRefreshRejectedAsync(tokens.RefreshToken);
        await AssertRefreshRejectedAsync("not-a-token");
    }

    [Fact]
    public async Task RevokingASessionRevokesTheRefreshTokensItHeldBeforeItsLatestRefresh()
    {
        var signIn = await LoginAsync(Alice);
        var refreshed = await RefreshTokensAsync(signIn.RefreshToken);

        await LogoutAsync(refreshed.AccessToken, refreshed.RefreshToken);

        await AssertRefreshRejectedAsync(signIn.RefreshToken);
        await AssertRefreshRejectedAsync(refreshed.RefreshToken);
    }

    [Fact]
    public async Task OtherSessionsOfTheSameUserAreUnaffected()
    {
        var ended = await LoginAsync(Alice);
        var other = await LoginAsync(Alice);

        await AssertLogoutAsync(HttpStatusCode.NoContent, ended.AccessToken, ended.RefreshToken);

        await AssertRefreshRejectedAsync(ended.RefreshToken);
        await AssertRefreshWorksAsync(other.RefreshToken);
    }

    [Fact]
    public async Task RevokingARevokedTokenSucceeds()
    {
        var tokens = await LoginAsync(Alice);

        await AssertLogoutAsync(HttpStatusCode.NoContent, tokens.AccessToken, tokens.RefreshToken);
        await AssertLogoutAsync(HttpStatusCode.NoContent, tokens.AccessToken, tokens.RefreshToken);
        await AssertRefreshRejectedAsync(tokens.RefreshToken);
    }

    [Fact]
    public async Task RevokingAnUnknownTokenSucceedsWithoutEndingTheSession()
    {
        var tokens = await LoginAsync(Alice);
        // Well-formed refresh token, but signed with a key this deployment does not use.
        var signedWithAnotherKey = LegacyRefreshToken.Create(new() { SigningKey = "another-signing-key-with-at-least-32-chars" }, Alice);

        await AssertLogoutAsync(HttpStatusCode.NoContent, tokens.AccessToken, "not-a-token");
        await AssertLogoutAsync(HttpStatusCode.NoContent, tokens.AccessToken, signedWithAnotherKey);

        await AssertRefreshWorksAsync(tokens.RefreshToken);
    }

    [Fact]
    public async Task AnAccessTokenIsRefusedRatherThanReportedRevoked()
    {
        var tokens = await LoginAsync(Alice);

        await AssertLogoutAsync(HttpStatusCode.BadRequest, tokens.AccessToken, tokens.AccessToken);

        await AssertRefreshWorksAsync(tokens.RefreshToken);
    }

    [Fact]
    public async Task AMissingRefreshTokenIsRefused()
    {
        var tokens = await LoginAsync(Alice);

        await AssertLogoutAsync(HttpStatusCode.BadRequest, tokens.AccessToken, "");
    }

    [Fact]
    public async Task AnotherUsersSessionIsNotRevoked()
    {
        var alice = await LoginAsync(Alice);
        var bob = await LoginAsync(Bob);

        await AssertLogoutAsync(HttpStatusCode.Forbidden, alice.AccessToken, bob.RefreshToken);

        await AssertRefreshWorksAsync(bob.RefreshToken);
    }

    [Fact]
    public async Task ARefreshTokenOfTheSameUserInAnotherTenantIsNotRevoked()
    {
        var alice = await LoginAsync(Alice);
        var otherTenant = await IssueTokensAsync(new() { Id = Alice.Id, Name = Alice.Name, TenantId = "tenant-b" });

        await AssertLogoutAsync(HttpStatusCode.Forbidden, alice.AccessToken, otherTenant.RefreshToken);

        await using var scope = _app.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<SessionRevoker>().IsRevokedAsync(GetSession(otherTenant.RefreshToken).Id));
    }

    [Fact]
    public async Task LogoutRequiresAnAccessToken()
    {
        var tokens = await LoginAsync(Alice);

        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync("/identity/logout", null, tokens.RefreshToken)).StatusCode);
        await AssertLogoutAsync(HttpStatusCode.Unauthorized, tokens.RefreshToken, tokens.RefreshToken);

        await AssertRefreshWorksAsync(tokens.RefreshToken);
    }

    [Fact]
    public async Task RevokingWithAnExpiredRefreshTokenEndsItsSession()
    {
        _clock.UtcNow -= TimeSpan.FromHours(3);
        var expired = await LoginAsync(Alice);
        _clock.UtcNow += TimeSpan.FromHours(3);
        var live = await ContinueSessionAsync(Alice, expired.RefreshToken);
        await AssertRefreshWorksAsync(live.RefreshToken);

        await AssertLogoutAsync(HttpStatusCode.NoContent, live.AccessToken, expired.RefreshToken);

        await AssertRefreshRejectedAsync(live.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenIssuedBeforeSessionsExistedWorksUntilItsSessionIsRevoked()
    {
        var legacy = LegacyRefreshToken.Create(_tokenOptions, Alice);
        var refreshed = await RefreshTokensAsync(legacy);

        await AssertLogoutAsync(HttpStatusCode.NoContent, refreshed.AccessToken, legacy);

        await AssertRefreshRejectedAsync(legacy);
        await AssertRefreshRejectedAsync(refreshed.RefreshToken);
    }

    [Fact]
    public async Task ARevocationOutlivesARefreshTokenIssuedBeforeSessionsExistedWithALongerLifetime()
    {
        _tokenOptions.RefreshTokenLifetime = TimeSpan.FromDays(30);
        var legacy = LegacyRefreshToken.Create(_tokenOptions, Alice);
        _tokenOptions.RefreshTokenLifetime = TimeSpan.FromHours(2);
        var refreshed = await RefreshTokensAsync(legacy);

        await AssertLogoutAsync(HttpStatusCode.NoContent, refreshed.AccessToken, refreshed.RefreshToken);
        await PruneRevocationsAsync(TimeSpan.FromDays(1));

        await AssertRefreshRejectedAsync(legacy);
    }

    [Fact]
    public async Task ARevocationOutlivesTheOlderRefreshTokensOfTheSessionAfterTheLifetimeIsLowered()
    {
        _tokenOptions.RefreshTokenLifetime = TimeSpan.FromDays(30);
        var signIn = await LoginAsync(Alice);
        _tokenOptions.RefreshTokenLifetime = TimeSpan.FromHours(2);
        var refreshed = await RefreshTokensAsync(signIn.RefreshToken);

        await AssertLogoutAsync(HttpStatusCode.NoContent, refreshed.AccessToken, refreshed.RefreshToken);
        await AssertRefreshRejectedAsync(signIn.RefreshToken);

        // Long enough for a revocation computed from the current lifetime alone to be pruned.
        await PruneRevocationsAsync(TimeSpan.FromDays(1));

        await AssertRefreshRejectedAsync(signIn.RefreshToken);
    }

    [Fact]
    public async Task SigningOutAgainWithAShorterLivedRefreshTokenDoesNotShortenTheRevocation()
    {
        var signIn = await LoginAsync(Alice);
        _tokenOptions.RefreshTokenLifetime = TimeSpan.FromDays(30);
        var longLived = await RefreshTokensAsync(signIn.RefreshToken);
        _tokenOptions.RefreshTokenLifetime = TimeSpan.FromHours(2);

        await AssertLogoutAsync(HttpStatusCode.NoContent, longLived.AccessToken, longLived.RefreshToken);
        // The sign-in token knows nothing of the longer-lived one refreshed from it.
        await AssertLogoutAsync(HttpStatusCode.NoContent, longLived.AccessToken, signIn.RefreshToken);
        await PruneRevocationsAsync(TimeSpan.FromDays(1));

        await AssertRefreshRejectedAsync(longLived.RefreshToken);
    }

    private async Task<IssuedTokens> LoginAsync(User user) =>
        await ReadTokensAsync(await _client.PostAsJsonAsync("/identity/login", new { username = user.Name, password = "unchecked" }));

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) => SendAsync("/identity/refresh-token", refreshToken, null);

    private Task<HttpResponseMessage> LogoutAsync(string accessToken, string refreshToken) => SendAsync("/identity/logout", accessToken, refreshToken);

    private async Task<IssuedTokens> RefreshTokensAsync(string refreshToken) => await ReadTokensAsync(await RefreshAsync(refreshToken));

    private Task AssertRefreshWorksAsync(string refreshToken) => RefreshTokensAsync(refreshToken);

    private async Task AssertRefreshRejectedAsync(string refreshToken) => Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(refreshToken)).StatusCode);

    private async Task AssertLogoutAsync(HttpStatusCode expected, string accessToken, string refreshToken) => Assert.Equal(expected, (await LogoutAsync(accessToken, refreshToken)).StatusCode);

    private async Task<HttpResponseMessage> SendAsync(string path, string? bearerToken, string? refreshTokenToRevoke)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);

        if (bearerToken != null)
            request.Headers.Authorization = new("Bearer", bearerToken);

        if (refreshTokenToRevoke != null)
            request.Content = JsonContent.Create(new { refreshToken = refreshTokenToRevoke });

        return await _client.SendAsync(request);
    }

    // Revocations are pruned when the next session is revoked.
    private async Task PruneRevocationsAsync(TimeSpan later)
    {
        _clock.UtcNow += later;
        var other = await LoginAsync(Bob);
        await AssertLogoutAsync(HttpStatusCode.NoContent, other.AccessToken, other.RefreshToken);
    }

    private Task<IssuedTokens> ContinueSessionAsync(User user, string refreshToken) => IssueTokensAsync(user, GetSession(refreshToken));

    private async Task<IssuedTokens> IssueTokensAsync(User user, SignInSession? session = null)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>().IssueTokensAsync(user, session);
    }

    private static SignInSession GetSession(string refreshToken) =>
        SessionRevoker.GetSession(new ClaimsIdentity(new JsonWebTokenHandler().ReadJsonWebToken(refreshToken).Claims), refreshToken);

    private static async Task<IssuedTokens> ReadTokensAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.True(body.IsAuthenticated);
        return new(body.AccessToken!, body.RefreshToken!);
    }

    // Password verification is not under test, and costs 600,000 PBKDF2 iterations per sign-in.
    private sealed class NameOnlyCredentialsValidator(IUserProvider userProvider) : IUserCredentialsValidator
    {
        public async ValueTask<User?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default) =>
            await userProvider.FindByNameAsync(username, cancellationToken);
    }
}
