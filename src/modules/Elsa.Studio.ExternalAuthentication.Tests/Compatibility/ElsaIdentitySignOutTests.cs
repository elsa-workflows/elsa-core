using System.Text;
using System.Text.Json;
using Bunit;
using Bunit.TestDoubles;
using Elsa.Studio.Authentication.ElsaIdentity;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.Services;
using Elsa.Studio.Authentication.ElsaIdentity.UI;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Components;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

/// <summary>
/// The ElsaIdentity (username/password) provider contributes the same app bar user menu as the broker, with a
/// sign-out entry that ends the local session.
/// </summary>
public sealed class ElsaIdentitySignOutTests : AppBarUserMenuTests<ElsaIdentityUIFeature, ElsaIdentityUserMenu>
{
    private readonly InMemoryJwtAccessor _tokens = new();

    public ElsaIdentitySignOutTests()
    {
        Services.AddElsaIdentityCore();
        Services.AddElsaIdentityUI();
        Services.AddSingleton<IJwtAccessor>(_tokens);
    }

    [Fact]
    public void SignOut_ClearsTheSessionAndReturnsToTheLoginPage()
    {
        SignIn();
        _tokens.Tokens[TokenNames.IdToken] = "stale-id-token";
        var stateChanges = new List<AuthenticationState>();
        Services.GetRequiredService<AuthenticationStateProvider>().AuthenticationStateChanged +=
            async state => stateChanges.Add(await state);
        var menu = RenderAppBarMenu();

        menu.Find(".mud-menu button").Click();
        PopoverProvider!.WaitForElements(".mud-menu-item")
            .Single(item => item.TextContent.Trim() == "Sign out")
            .Click();

        var history = Services.GetRequiredService<BunitNavigationManager>().History;
        menu.WaitForAssertion(() =>
        {
            Assert.Empty(menu.FindAll(".mud-menu"));
            Assert.NotEmpty(history);
        });
        Assert.Empty(_tokens.Tokens);
        Assert.False(Assert.Single(stateChanges).User.Identity?.IsAuthenticated);
        var navigation = Assert.Single(history);
        Assert.Equal("/login", navigation.Uri);
        Assert.True(navigation.Options.ForceLoad);
    }

    [Fact]
    public async Task RefreshCompletingAfterSignOut_DoesNotRestoreTheSession()
    {
        SignIn();
        var refreshResponse = new PausedHandler();
        var refreshTokenService = CreateRefreshTokenService(refreshResponse);

        var refresh = refreshTokenService.RefreshTokenAsync(CancellationToken.None);
        await Services.GetRequiredService<ISignOutService>().SignOutAsync();
        refreshResponse.Respond("""{ "isAuthenticated": true, "accessToken": "new-access", "refreshToken": "new-refresh" }""");

        Assert.False((await refresh).IsAuthenticated);
        Assert.Empty(_tokens.Tokens);
    }

    [Fact]
    public async Task SignOutWhileARefreshStoresItsTokens_StillEndsTheSession()
    {
        SignIn();
        var refreshResponse = new PausedHandler();
        var refreshTokenService = CreateRefreshTokenService(refreshResponse);
        var pausedWrite = _tokens.PauseNextWrite();

        var refresh = refreshTokenService.RefreshTokenAsync(CancellationToken.None);
        refreshResponse.Respond("""{ "isAuthenticated": true, "accessToken": "new-access", "refreshToken": "new-refresh" }""");
        await pausedWrite.Started;
        var signOut = Services.GetRequiredService<ISignOutService>().SignOutAsync();
        pausedWrite.Release();
        await refresh;
        await signOut;

        Assert.Empty(_tokens.Tokens);
    }

    [Fact]
    public async Task RefreshOutlivedBySignInInAnotherTab_KeepsTheNewerSession()
    {
        _tokens.Tokens[TokenNames.AccessToken] = CreateJwt("alice", TimeSpan.FromMinutes(-5));
        _tokens.Tokens[TokenNames.RefreshToken] = "refresh-token";
        var refreshResponse = new PausedHandler();
        var tokenProvider = new JwtTokenProvider(_tokens, new JwtParser(), new SingleFlightCoordinator(), CreateRefreshTokenService(refreshResponse));

        var accessToken = tokenProvider.GetAccessTokenAsync();
        var bobAccessToken = CreateJwt("bob");
        _tokens.Tokens[TokenNames.AccessToken] = bobAccessToken;
        _tokens.Tokens[TokenNames.RefreshToken] = "bob-refresh-token";
        refreshResponse.Respond("""{ "isAuthenticated": true, "accessToken": "alice-access", "refreshToken": "alice-refresh" }""");

        Assert.Equal(bobAccessToken, await accessToken);
        Assert.Equal(bobAccessToken, _tokens.Tokens[TokenNames.AccessToken]);
        Assert.Equal("bob-refresh-token", _tokens.Tokens[TokenNames.RefreshToken]);
    }

    private ElsaIdentityRefreshTokenService CreateRefreshTokenService(HttpMessageHandler handler) =>
        new(new StaticRemoteBackendAccessor(), _tokens, new StaticHttpClientFactory(handler), Services.GetRequiredService<ElsaIdentitySessionGate>());

    protected override void SignIn()
    {
        _tokens.Tokens[TokenNames.AccessToken] = CreateJwt(UserName);
        _tokens.Tokens[TokenNames.RefreshToken] = "refresh-token";
    }

    private static string CreateJwt(string userName, TimeSpan? expiresIn = null)
    {
        var payload = JsonSerializer.Serialize(new
        {
            name = userName,
            exp = DateTimeOffset.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(1)).ToUnixTimeSeconds()
        });
        return $"{Base64Url("{\"alg\":\"none\"}")}.{Base64Url(payload)}.signature";
    }

    private static string Base64Url(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class PausedWrite
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public void Release() => _released.SetResult();

        public Task WaitAsync()
        {
            _started.SetResult();
            return _released.Task;
        }
    }

    private sealed class PausedHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource<HttpResponseMessage> _response = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Respond(string json) =>
            _response.SetResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _response.Task;
    }

    private sealed class StaticHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StaticRemoteBackendAccessor : IRemoteBackendAccessor
    {
        public Elsa.Studio.Models.RemoteBackend RemoteBackend { get; } = new(new Uri("https://backend.example"));
    }

    private sealed class InMemoryJwtAccessor : IJwtAccessor
    {
        private PausedWrite? _pausedWrite;

        public Dictionary<string, string> Tokens { get; } = new();

        public PausedWrite PauseNextWrite() => _pausedWrite = new();

        public ValueTask<string?> ReadTokenAsync(string name) => ValueTask.FromResult(Tokens.GetValueOrDefault(name));

        public async ValueTask WriteTokenAsync(string name, string token)
        {
            if (Interlocked.Exchange(ref _pausedWrite, null) is { } pausedWrite)
                await pausedWrite.WaitAsync();

            Tokens[name] = token;
        }

        public ValueTask ClearTokenAsync(string name)
        {
            Tokens.Remove(name);
            return ValueTask.CompletedTask;
        }
    }
}
