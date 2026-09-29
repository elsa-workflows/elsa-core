using System.Net;
using System.Net.Http.Json;
using Elsa.Common;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Features;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
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
        await ReadTokensAsync(await RefreshAsync(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, (await LogoutAsync(tokens.AccessToken, tokens.RefreshToken)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(tokens.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync("not-a-token")).StatusCode);
    }

    [Fact]
    public async Task RevokingASessionRevokesTheRefreshTokensItHeldBeforeItsLatestRefresh()
    {
        var signIn = await LoginAsync(Alice);
        var refreshed = await ReadTokensAsync(await RefreshAsync(signIn.RefreshToken));

        await LogoutAsync(refreshed.AccessToken, refreshed.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(signIn.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(refreshed.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task OtherSessionsOfTheSameUserAreUnaffected()
    {
        var ended = await LoginAsync(Alice);
        var other = await LoginAsync(Alice);

        await LogoutAsync(ended.AccessToken, ended.RefreshToken);

        await ReadTokensAsync(await RefreshAsync(other.RefreshToken));
    }

    [Fact]
    public async Task RevokingARevokedTokenSucceeds()
    {
        var tokens = await LoginAsync(Alice);

        Assert.Equal(HttpStatusCode.NoContent, (await LogoutAsync(tokens.AccessToken, tokens.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await LogoutAsync(tokens.AccessToken, tokens.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task RevokingAnUnknownTokenSucceedsWithoutEndingTheSession()
    {
        var tokens = await LoginAsync(Alice);
        var signedElsewhere = LegacyRefreshToken.Create(new() { SigningKey = "another-signing-key-with-at-least-32-chars" }, Alice);

        Assert.Equal(HttpStatusCode.NoContent, (await LogoutAsync(tokens.AccessToken, "not-a-token")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await LogoutAsync(tokens.AccessToken, signedElsewhere)).StatusCode);

        await ReadTokensAsync(await RefreshAsync(tokens.RefreshToken));
    }

    [Fact]
    public async Task AnAccessTokenIsRefusedRatherThanReportedRevoked()
    {
        var tokens = await LoginAsync(Alice);

        Assert.Equal(HttpStatusCode.BadRequest, (await LogoutAsync(tokens.AccessToken, tokens.AccessToken)).StatusCode);

        await ReadTokensAsync(await RefreshAsync(tokens.RefreshToken));
    }

    [Fact]
    public async Task AMissingRefreshTokenIsRefused()
    {
        var tokens = await LoginAsync(Alice);

        Assert.Equal(HttpStatusCode.BadRequest, (await LogoutAsync(tokens.AccessToken, "")).StatusCode);
    }

    [Fact]
    public async Task AnotherUsersSessionIsNotRevoked()
    {
        var alice = await LoginAsync(Alice);
        var bob = await LoginAsync(Bob);

        Assert.Equal(HttpStatusCode.Forbidden, (await LogoutAsync(alice.AccessToken, bob.RefreshToken)).StatusCode);

        await ReadTokensAsync(await RefreshAsync(bob.RefreshToken));
    }

    [Fact]
    public async Task LogoutRequiresAnAccessToken()
    {
        var tokens = await LoginAsync(Alice);

        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync("/identity/logout", null, tokens.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LogoutAsync(tokens.RefreshToken, tokens.RefreshToken)).StatusCode);

        await ReadTokensAsync(await RefreshAsync(tokens.RefreshToken));
    }

    [Fact]
    public async Task RevokingWithAnExpiredRefreshTokenEndsItsSession()
    {
        _clock.UtcNow -= TimeSpan.FromHours(3);
        var expired = await LoginAsync(Alice);
        _clock.UtcNow += TimeSpan.FromHours(3);
        var live = await ContinueSessionAsync(Alice, expired.RefreshToken);
        await ReadTokensAsync(await RefreshAsync(live.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, (await LogoutAsync(live.AccessToken, expired.RefreshToken)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(live.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task RefreshTokenIssuedBeforeSessionsExistedWorksUntilItsSessionIsRevoked()
    {
        var legacy = LegacyRefreshToken.Create(_app.Services.GetRequiredService<IOptions<IdentityTokenOptions>>().Value, Alice);
        var refreshed = await ReadTokensAsync(await RefreshAsync(legacy));

        Assert.Equal(HttpStatusCode.NoContent, (await LogoutAsync(refreshed.AccessToken, legacy)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(legacy)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(refreshed.RefreshToken)).StatusCode);
    }

    private async Task<IssuedTokens> LoginAsync(User user) =>
        await ReadTokensAsync(await _client.PostAsJsonAsync("/identity/login", new { username = user.Name, password = "unchecked" }));

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) => SendAsync("/identity/refresh-token", refreshToken, null);

    private Task<HttpResponseMessage> LogoutAsync(string accessToken, string refreshToken) => SendAsync("/identity/logout", accessToken, refreshToken);

    private async Task<HttpResponseMessage> SendAsync(string path, string? bearerToken, string? refreshTokenToRevoke)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);

        if (bearerToken != null)
            request.Headers.Authorization = new("Bearer", bearerToken);

        if (refreshTokenToRevoke != null)
            request.Content = JsonContent.Create(new { refreshToken = refreshTokenToRevoke });

        return await _client.SendAsync(request);
    }

    private async Task<IssuedTokens> ContinueSessionAsync(User user, string refreshToken)
    {
        var sessionId = new JsonWebTokenHandler().ReadJsonWebToken(refreshToken).GetClaim(CustomClaimTypes.SessionId).Value;
        await using var scope = _app.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>().IssueTokensAsync(user, sessionId);
    }

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
