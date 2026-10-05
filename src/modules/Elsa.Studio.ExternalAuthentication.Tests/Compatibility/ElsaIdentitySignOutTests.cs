using System.Net;
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
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

/// <summary>
/// The ElsaIdentity (username/password) provider contributes the same app bar user menu as the broker, with a
/// sign-out entry that ends the local session, after asking the backend to revoke it.
/// </summary>
public sealed class ElsaIdentitySignOutTests : AppBarUserMenuTests<ElsaIdentityUIFeature, ElsaIdentityUserMenu>
{
    private const string RefreshEndpoint = "POST /elsa/api/identity/refresh-token";
    private const string LogoutEndpoint = "POST /elsa/api/identity/logout";

    private readonly InMemoryJwtAccessor _tokens = new();
    private readonly FakeBackend _backend;
    private readonly ExpirableTimeProvider _time = new();
    private readonly WarningLog _warnings = new();

    public ElsaIdentitySignOutTests()
    {
        _backend = new(_tokens);
        Services.AddElsaIdentityCore();
        Services.AddElsaIdentityUI();
        Services.AddSingleton<IJwtAccessor>(_tokens);
        Services.AddSingleton<IRemoteBackendAccessor, StaticRemoteBackendAccessor>();
        Services.AddSingleton<IHttpClientFactory>(new StaticHttpClientFactory(_backend));
        Services.AddScoped<ISingleFlightCoordinator, SingleFlightCoordinator>();
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddLogging(logging => logging.AddProvider(_warnings));
    }

    [Fact]
    public async Task SignOut_ClearsTheSessionAndReturnsToTheLoginPage()
    {
        SignIn();
        _tokens.Tokens[TokenNames.IdToken] = "stale-id-token";
        var stateChanges = new List<AuthenticationState>();
        Services.GetRequiredService<AuthenticationStateProvider>().AuthenticationStateChanged +=
            async state => stateChanges.Add(await state);
        var menu = RenderAppBarMenu();

        await ClickSignOutAsync(menu);

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
    public async Task SignOut_RevokesTheSessionBeforeClearingTheTokens()
    {
        SignIn();
        var accessToken = _tokens.Tokens[TokenNames.AccessToken];

        await SignOutAsync();

        var logout = Assert.Single(_backend.Requests);
        Assert.Equal(LogoutEndpoint, logout.Endpoint);
        Assert.Equal($"Bearer {accessToken}", logout.Authorization);
        Assert.Equal("""{"refreshToken":"refresh-token"}""", logout.Body);
        Assert.Equal("refresh-token", logout.Tokens[TokenNames.RefreshToken]);
        Assert.Empty(_warnings.Messages);
        AssertSignedOutLocally();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SignOut_WhenTheBackendRefusesToRevoke_StillEndsTheSession(HttpStatusCode status)
    {
        SignIn();
        _backend.OnLogout = (_, _) => Task.FromResult(new HttpResponseMessage(status));

        await SignOutAsync();

        Assert.Single(_backend.Requests);
        Assert.Single(_warnings.Messages);
        AssertSignedOutLocally();
    }

    [Fact]
    public async Task SignOut_WhenTheBackendIsUnreachable_StillEndsTheSession()
    {
        SignIn();
        _backend.OnLogout = (_, _) => throw new HttpRequestException("Connection refused");

        await SignOutAsync();

        Assert.Single(_warnings.Messages);
        AssertSignedOutLocally();
    }

    [Fact]
    public async Task SignOut_WhenTheBackendDoesNotAnswerInTime_StillEndsTheSession()
    {
        SignIn();
        _time.TimersExpireImmediately = true;
        _backend.OnLogout = async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage();
        };

        await SignOutAsync();

        Assert.Single(_warnings.Messages);
        AssertSignedOutLocally();
    }

    [Fact]
    public async Task SignOut_WhenTheTokenStoreDoesNotAnswerInTime_StillEndsTheSession()
    {
        SignIn();
        _time.TimersExpireImmediately = true;
        _tokens.StallReads = true;

        await SignOutAsync();

        Assert.Empty(_backend.Requests);
        Assert.Single(_warnings.Messages);
        AssertSignedOutLocally();
    }

    // The token provider refreshes a token that is about to expire, and a rejected refresh clears the stored tokens.
    // Signing out must not lose a still-valid access token that way, or the backend session would stay active.
    [Fact]
    public async Task SignOut_WhenTheAccessTokenIsAboutToExpire_RevokesWithItInsteadOfRefreshing()
    {
        SignIn();
        var accessToken = _tokens.Tokens[TokenNames.AccessToken] = CreateJwt(UserName, TimeSpan.FromSeconds(90));
        _backend.OnRefresh = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        await SignOutAsync();

        var logout = Assert.Single(_backend.Requests);
        Assert.Equal(LogoutEndpoint, logout.Endpoint);
        Assert.Equal($"Bearer {accessToken}", logout.Authorization);
        AssertSignedOutLocally();
    }

    [Fact]
    public async Task SignOut_WhenNoRefreshTokenIsStored_StillEndsTheSessionWithoutRevoking()
    {
        SignIn();
        _tokens.Tokens.Remove(TokenNames.RefreshToken);

        await SignOutAsync();

        Assert.Empty(_backend.Requests);
        Assert.Empty(_warnings.Messages);
        AssertSignedOutLocally();
    }

    [Fact]
    public async Task SignOut_WhenTheRefreshDoesNotAnswerInTime_StillEndsTheSessionWithoutRevoking()
    {
        SignInWithExpiredAccessToken();
        _time.TimersExpireImmediately = true;
        _backend.OnRefresh = async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage();
        };

        await SignOutAsync();

        Assert.DoesNotContain(_backend.Requests, request => request.Endpoint == LogoutEndpoint);
        AssertSignedOutLocally();
    }

    [Fact]
    public async Task SignOut_WhenClickedAgainWhileRevoking_SignsOutOnlyOnce()
    {
        SignIn();
        var revoking = new TaskCompletionSource();
        var revoked = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _backend.OnLogout = (_, _) =>
        {
            revoking.TrySetResult();
            return revoked.Task;
        };
        var menu = RenderAppBarMenu();

        await ClickSignOutAsync(menu);
        await revoking.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await ClickSignOutAsync(menu);
        revoked.SetResult(new HttpResponseMessage(HttpStatusCode.NoContent));

        menu.WaitForAssertion(() => Assert.NotEmpty(Services.GetRequiredService<BunitNavigationManager>().History));
        Assert.Single(_backend.Requests);
        AssertSignedOutLocally();
    }

    [Fact]
    public async Task SignOut_WhenTheAccessTokenIsExpired_RefreshesFirstAndRevokesWithTheNewTokens()
    {
        SignInWithExpiredAccessToken();
        _backend.OnRefresh = (_, _) => Task.FromResult(RefreshedTokens());

        await SignOutAsync();

        Assert.Equal([RefreshEndpoint, LogoutEndpoint], _backend.Requests.Select(request => request.Endpoint));
        var logout = _backend.Requests[1];
        Assert.Equal("Bearer new-access", logout.Authorization);
        Assert.Equal("""{"refreshToken":"new-refresh"}""", logout.Body);
        Assert.Equal("new-refresh", logout.Tokens[TokenNames.RefreshToken]);
        AssertSignedOutLocally();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task SignOut_WhenTheRefreshFails_StillEndsTheSessionWithoutRevoking(HttpStatusCode status)
    {
        SignInWithExpiredAccessToken();
        _backend.OnRefresh = (_, _) => Task.FromResult(new HttpResponseMessage(status));

        await SignOutAsync();

        Assert.DoesNotContain(_backend.Requests, request => request.Endpoint == LogoutEndpoint);
        AssertSignedOutLocally();
    }

    [Fact]
    public async Task SignOut_WhenTheRefreshCannotReachTheBackend_StillEndsTheSessionWithoutRevoking()
    {
        SignInWithExpiredAccessToken();
        _backend.OnRefresh = (_, _) => throw new HttpRequestException("Connection refused");

        await SignOutAsync();

        Assert.DoesNotContain(_backend.Requests, request => request.Endpoint == LogoutEndpoint);
        AssertSignedOutLocally();
    }

    [Fact]
    public async Task SignOut_WhileAnotherRequestRefreshes_WaitsForItAndRevokesWithTheNewTokens()
    {
        SignInWithExpiredAccessToken();
        var refreshing = new TaskCompletionSource();
        _backend.OnRefresh = async (_, _) =>
        {
            await refreshing.Task;
            return RefreshedTokens();
        };

        var accessToken = Services.GetRequiredService<ITokenProvider>().GetAccessTokenAsync();
        var signOut = SignOutAsync();
        refreshing.SetResult();
        await Task.WhenAll(accessToken, signOut).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Empty(_warnings.Messages);
        var logout = Assert.Single(_backend.Requests, request => request.Endpoint == LogoutEndpoint);
        Assert.Same(logout, _backend.Requests[^1]);
        Assert.Equal("Bearer new-access", logout.Authorization);
        AssertSignedOutLocally();
    }

    [Fact]
    public async Task RefreshCompletingWhileTheSessionIsBeingRevoked_DoesNotBlockOrSurviveSignOut()
    {
        SignIn();
        var revoking = new TaskCompletionSource();
        var revoked = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _backend.OnLogout = (_, _) =>
        {
            revoking.SetResult();
            return revoked.Task;
        };
        _backend.OnRefresh = (_, _) => refreshResponse.Task;

        var refresh = Services.GetRequiredService<IRefreshTokenService>().RefreshTokenAsync(CancellationToken.None);
        var signOut = SignOutAsync();
        await revoking.Task.WaitAsync(TimeSpan.FromSeconds(10));
        refreshResponse.SetResult(RefreshedTokens());
        await refresh.WaitAsync(TimeSpan.FromSeconds(10));
        revoked.SetResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        await signOut.WaitAsync(TimeSpan.FromSeconds(10));

        AssertSignedOutLocally();
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
    public async Task SignOut_WhileARefreshStoresItsTokens_StillEndsTheSession()
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

    private void SignInWithExpiredAccessToken()
    {
        SignIn();
        _tokens.Tokens[TokenNames.AccessToken] = CreateJwt(UserName, TimeSpan.FromMinutes(-5));
    }

    /// <summary>Opens the menu and clicks its sign-out item, which closes the menu again.</summary>
    private async Task ClickSignOutAsync(IRenderedComponent<ElsaIdentityUserMenu> menu)
    {
        // Each element is found and clicked on the renderer's own context, so no render can replace it in between: the
        // menu re-renders when it closes and when signing out turns busy.
        await menu.InvokeAsync(() => menu.Find(".mud-menu button").Click());
        PopoverProvider!.WaitForElements(".mud-menu-item");
        await PopoverProvider.InvokeAsync(() => PopoverProvider.FindAll(".mud-menu-item")
            .Single(item => item.TextContent.Trim() == "Sign out")
            .Click());
    }

    private Task SignOutAsync() => Services.GetRequiredService<ISignOutService>().SignOutAsync();

    private void AssertSignedOutLocally()
    {
        Assert.Empty(_tokens.Tokens);
        var navigation = Assert.Single(Services.GetRequiredService<BunitNavigationManager>().History);
        Assert.Equal("/login", navigation.Uri);
        Assert.True(navigation.Options.ForceLoad);
    }

    private static HttpResponseMessage RefreshedTokens() => Json("""{ "isAuthenticated": true, "accessToken": "new-access", "refreshToken": "new-refresh" }""");

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

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

        public void Respond(string json) => _response.SetResult(Json(json));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _response.Task;
    }

    private sealed record RecordedRequest(string Endpoint, string? Authorization, string Body, Dictionary<string, string> Tokens);

    /// <summary>Answers the identity endpoints, recording each request with the tokens stored while it was in flight.</summary>
    private sealed class FakeBackend(InMemoryJwtAccessor tokens) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnRefresh { get; set; } = (_, _) => throw new NotSupportedException();

        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnLogout { get; set; } = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new($"{request.Method} {request.RequestUri!.AbsolutePath}", request.Headers.Authorization?.ToString(), body, new(tokens.Tokens)));
            return await (request.RequestUri.AbsolutePath.EndsWith("/identity/logout") ? OnLogout : OnRefresh)(request, cancellationToken);
        }
    }

    /// <summary>A time provider whose timers can be made to fire at once, so a timeout does not have to be waited out.</summary>
    private sealed class ExpirableTimeProvider : TimeProvider
    {
        public bool TimersExpireImmediately { get; set; }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            base.CreateTimer(callback, state, TimersExpireImmediately ? TimeSpan.Zero : dueTime, period);
    }

    private sealed class WarningLog : ILoggerProvider, ILogger
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
                Messages.Add(formatter(state, exception));
        }

        public void Dispose()
        {
        }
    }

    private sealed class StaticHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StaticRemoteBackendAccessor : IRemoteBackendAccessor
    {
        public Elsa.Studio.Models.RemoteBackend RemoteBackend { get; } = new(new Uri("https://backend.example/elsa/api"));
    }

    private sealed class InMemoryJwtAccessor : IJwtAccessor
    {
        private PausedWrite? _pausedWrite;

        public Dictionary<string, string> Tokens { get; } = new();

        public PausedWrite PauseNextWrite() => _pausedWrite = new();

        /// <summary>Makes reads never complete, like browser storage that does not answer.</summary>
        public bool StallReads { get; set; }

        public ValueTask<string?> ReadTokenAsync(string name) =>
            StallReads ? new(new TaskCompletionSource<string?>().Task) : ValueTask.FromResult(Tokens.GetValueOrDefault(name));

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
