using System.Text.Json;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Extensions;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.BlazorWasm;

public class ExternalAuthenticationWasmTests
{
    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("/workflows?version=1", "/workflows?version=1")]
    [InlineData("https://evil.example/", "/")]
    [InlineData("//evil.example/", "/")]
    [InlineData("/\\evil", "/")]
    public void ReturnPathsRemainClientLocal(string? candidate, string expected) =>
        Assert.Equal(expected, ExternalAuthenticationReturnPath.Normalize(candidate));

    [Fact]
    public async Task DefaultMemoryStorageDoesNotUseBrowserPersistence()
    {
        var store = new BrowserExternalAuthenticationTokenStore(new ThrowingJsRuntime(), new());
        var tokens = Tokens();

        await store.SetAsync(tokens);

        Assert.Equal(tokens, await store.GetAsync());
        await store.ClearAsync();
        Assert.Null(await store.GetAsync());
    }

    [Theory]
    [InlineData(ExternalAuthenticationBrowserStorageMode.Session, "sessionStorage.setItem")]
    [InlineData(ExternalAuthenticationBrowserStorageMode.Durable, "localStorage.setItem")]
    public async Task ExplicitPersistentStorageUsesTheSelectedBrowserStore(
        ExternalAuthenticationBrowserStorageMode mode,
        string expectedOperation)
    {
        var js = new BrowserStorageJsRuntime();
        var store = new BrowserExternalAuthenticationTokenStore(js, new() { BrowserStorage = mode });

        await store.SetAsync(Tokens());

        Assert.Contains(expectedOperation, js.Operations);
        Assert.NotNull(await store.GetAsync());
    }

    [Fact]
    public async Task PkceUsesBrowserCryptoAndStoresAnExpiringOneTimeTransaction()
    {
        var js = new BrowserStorageJsRuntime();
        var transactions = new BrowserExternalAuthenticationPkceTransactionStore(js);
        var service = new BrowserExternalAuthenticationPkceService(js, transactions);

        var (transaction, challenge) = await service.CreateAsync("/workflows");

        Assert.Equal("state-value", transaction.State);
        Assert.Equal("verifier-value", transaction.CodeVerifier);
        Assert.Equal("challenge-value", challenge);
        Assert.Equal("/workflows", transaction.ReturnPath);
        Assert.True(transaction.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal(transaction, await transactions.TakeAsync(transaction.State));
        Assert.Null(await transactions.TakeAsync(transaction.State));
    }

    [Fact]
    public async Task ConcurrentRefreshUsesOneRotatedRefreshToken()
    {
        var store = new BrowserExternalAuthenticationTokenStore(new ThrowingJsRuntime(), new());
        await store.SetAsync(Tokens(accessExpiresAt: DateTimeOffset.UtcNow.AddSeconds(-1)));
        var broker = new RecordingBrokerApi();
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        broker.OnExchange = async request =>
        {
            Assert.Equal("refresh_token", request.GrantType);
            refreshStarted.TrySetResult();
            await releaseRefresh.Task;
            return Response("access-rotated", "refresh-rotated");
        };
        var provider = CreateTokenProvider(store, broker);

        var first = provider.GetAccessTokenAsync();
        await refreshStarted.Task;
        var second = provider.GetAccessTokenAsync();
        releaseRefresh.TrySetResult();

        Assert.Equal("access-rotated", await first);
        Assert.Equal("access-rotated", await second);
        Assert.Equal(1, broker.ExchangeCount);
        Assert.Equal("refresh-rotated", (await store.GetAsync())!.RefreshToken);
    }

    [Fact]
    public async Task ExternalSessionExpiryBoundsAccessAndRefreshLifetime()
    {
        var store = new BrowserExternalAuthenticationTokenStore(new ThrowingJsRuntime(), new());
        var provider = CreateTokenProvider(store, new RecordingBrokerApi());
        var before = DateTimeOffset.UtcNow;

        await provider.SetAsync(new("access", "Bearer", 3600, "refresh", 3600, 1));

        var tokens = await store.GetAsync();
        Assert.NotNull(tokens);
        Assert.True(tokens!.AccessTokenExpiresAt <= before.AddSeconds(2));
        Assert.True(tokens.RefreshTokenExpiresAt <= before.AddSeconds(2));
        Assert.True(tokens.ExternalSessionExpiresAt <= before.AddSeconds(2));
    }

    [Fact]
    public async Task LocalBrokerTokensWithoutAnExternalSessionLifetimeRemainUsableUntilTheirAccessTokenExpires()
    {
        var store = new BrowserExternalAuthenticationTokenStore(new ThrowingJsRuntime(), new());
        var broker = new RecordingBrokerApi();
        var provider = CreateTokenProvider(store, broker);

        await provider.SetAsync(new("access", "Bearer", 3600, "refresh", 0, 0));

        Assert.Equal("access", await provider.GetAccessTokenAsync());
        Assert.Equal(0, broker.ExchangeCount);
    }

    [Fact]
    public async Task MissingCredentialsRemainAnonymousWithoutPublishingAChange()
    {
        var store = new BrowserExternalAuthenticationTokenStore(new ThrowingJsRuntime(), new());
        var provider = CreateTokenProvider(store, new RecordingBrokerApi());
        var changeCount = 0;
        provider.TokensChanged += () => changeCount++;

        Assert.Null(await provider.GetAccessTokenAsync());
        Assert.Equal(0, changeCount);
    }

    [Fact]
    public async Task CallbackProviderErrorConsumesTheTransaction()
    {
        var transactions = new RecordingTransactionStore(new("state", "verifier", "/workflows", DateTimeOffset.UtcNow.AddMinutes(1)));
        var service = CreateCallbackService(transactions);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(new Uri("https://studio.example/authentication/external/callback?state=state&error=access_denied")));

        Assert.Equal(1, transactions.TakeCount);
        Assert.Null(transactions.Current);
    }

    [Fact]
    public async Task MissingCodeConsumesStateAndPreventsCallbackReplay()
    {
        var transactions = new RecordingTransactionStore(new("state", "verifier", "/workflows", DateTimeOffset.UtcNow.AddMinutes(1)));
        var service = CreateCallbackService(transactions);
        var callback = new Uri("https://studio.example/authentication/external/callback?state=state");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(callback));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(new Uri("https://studio.example/authentication/external/callback?state=state&code=replay")));

        Assert.Equal(2, transactions.TakeCount);
        Assert.Null(transactions.Current);
    }

    [Fact]
    public async Task CallbackRejectsStaleStateAfterConsumingIt()
    {
        var transactions = new RecordingTransactionStore(new("state", "verifier", "/workflows", DateTimeOffset.UtcNow.AddSeconds(-1)));
        var service = CreateCallbackService(transactions);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(new Uri("https://studio.example/authentication/external/callback?state=state&code=code")));

        Assert.Equal(1, transactions.TakeCount);
        Assert.Null(transactions.Current);
    }

    [Fact]
    public async Task CallbackRequiresExactConfiguredOriginAndPath()
    {
        var transactions = new RecordingTransactionStore(new("state", "verifier", "/", DateTimeOffset.UtcNow.AddMinutes(1)));
        var service = CreateCallbackService(transactions);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(new Uri("https://evil.example/authentication/external/callback?state=state&code=code")));

        Assert.Equal(0, transactions.TakeCount);
    }

    [Fact]
    public async Task MalformedOrEmptyJwtDoesNotCreateAnAuthenticatedPrincipal()
    {
        var store = new BrowserExternalAuthenticationTokenStore(new ThrowingJsRuntime(), new());
        await store.SetAsync(Tokens(accessToken: "not-a-jwt"));
        var stateProvider = new ExternalAuthenticationWasmAuthenticationStateProvider(CreateTokenProvider(store, new RecordingBrokerApi()));

        var state = await stateProvider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Theory]
    [InlineData("local")]
    [InlineData("upstream")]
    public async Task LogoutAlwaysClearsLocalCredentialsAndUsesSelectedMode(string mode)
    {
        var store = new BrowserExternalAuthenticationTokenStore(new ThrowingJsRuntime(), new());
        await store.SetAsync(Tokens());
        var broker = new RecordingBrokerApi { LogoutResponse = new(false, "/external-authentication/logout/continue/one-time", null) };
        var tokenProvider = CreateTokenProvider(store, broker);
        var navigation = new TestNavigationManager();
        var service = new ExternalAuthenticationWasmLogoutService(new FakeAnonymousBackendApiClientProvider(broker), tokenProvider, navigation, Options());

        await service.LogoutAsync(mode);

        Assert.Equal(mode, broker.LogoutRequest?.Mode);
        Assert.StartsWith("Bearer ", broker.LogoutAuthorization, StringComparison.Ordinal);
        Assert.Null(await store.GetAsync());
        Assert.Equal("https://elsa.example/elsa/api/external-authentication/logout/continue/one-time", navigation.LastNavigation);
    }

    [Fact]
    public async Task ExternalLoginPreservesBackendPathWhenBaseUrlHasNoTrailingSlash()
    {
        var broker = new RecordingBrokerApi();
        var backend = new FakeAnonymousBackendApiClientProvider(
            broker,
            new Uri("https://elsa.example/elsa/api"));
        var navigation = new TestNavigationManager();
        var pkce = new BrowserExternalAuthenticationPkceService(
            new BrowserStorageJsRuntime(),
            new RecordingTransactionStore(null));
        var coordinator = new ExternalAuthenticationWasmLoginCoordinator(
            backend,
            pkce,
            navigation,
            Options());
        var method = new LoginMethodDescriptor(
            "keycloak-workforce",
            "keycloak-workforce",
            "external",
            "Keycloak",
            null,
            0,
            false,
            "/external-authentication/authorize/keycloak-workforce");

        await coordinator.BeginExternalAsync(method, "/");

        Assert.StartsWith(
            "https://elsa.example/elsa/api/external-authentication/authorize/keycloak-workforce?",
            navigation.LastNavigation,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PublicClientRejectsClientSecrets()
    {
        var services = new ServiceCollection();
        var exception = Assert.Throws<InvalidOperationException>(() => services.AddExternalAuthenticationBroker(options =>
        {
            options.ClientId = "studio";
            options.ClientSecret = "must-not-be-in-wasm";
        }));
        Assert.Contains("must not contain", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicClientRejectsCustomCallbackPaths()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddExternalAuthenticationBroker(options =>
        {
            options.ClientId = "studio";
            options.LogoutCallbackPath = "/custom-logout-callback";
        }));

        Assert.Contains("fixed Studio callback paths", exception.Message, StringComparison.Ordinal);
    }

    private static ExternalAuthenticationWasmCallbackService CreateCallbackService(RecordingTransactionStore transactions) =>
        new(new TestNavigationManager(), transactions, new FakeAnonymousBackendApiClientProvider(new RecordingBrokerApi()), CreateTokenProvider(new BrowserExternalAuthenticationTokenStore(new ThrowingJsRuntime(), new()), new RecordingBrokerApi()), Options());

    private static ExternalAuthenticationWasmTokenProvider CreateTokenProvider(IExternalAuthenticationBrowserTokenStore store, RecordingBrokerApi broker) =>
        new(store, new FakeAnonymousBackendApiClientProvider(broker), Options());

    private static ExternalAuthenticationWasmOptions Options() => new() { ClientId = "studio" };

    private static ExternalAuthenticationTokenSet Tokens(
        string accessToken = "access",
        DateTimeOffset? accessExpiresAt = null) => new(
        accessToken,
        "refresh",
        accessExpiresAt ?? DateTimeOffset.UtcNow.AddMinutes(10),
        DateTimeOffset.UtcNow.AddHours(1),
        DateTimeOffset.UtcNow.AddHours(1));

    private static BrokerTokenResponse Response(string accessToken, string refreshToken) => new(accessToken, "Bearer", 3600, refreshToken, 7200, 7200);

    private sealed class RecordingTransactionStore(ExternalAuthenticationPkceTransaction? transaction) : IExternalAuthenticationPkceTransactionStore
    {
        public ExternalAuthenticationPkceTransaction? Current { get; private set; } = transaction;
        public int TakeCount { get; private set; }
        public Task SaveAsync(ExternalAuthenticationPkceTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ExternalAuthenticationPkceTransaction?> TakeAsync(string state, CancellationToken cancellationToken = default)
        {
            TakeCount++;
            var transaction = Current;
            Current = null;
            return Task.FromResult(transaction);
        }
    }

    private sealed class RecordingBrokerApi : IExternalAuthenticationBrokerApi
    {
        public Func<BrokerTokenRequest, Task<BrokerTokenResponse>> OnExchange { get; set; } = _ => Task.FromResult(Response("access", "refresh"));
        public int ExchangeCount { get; private set; }
        public BrokerLogoutRequest? LogoutRequest { get; private set; }
        public string? LogoutAuthorization { get; private set; }
        public BrokerLogoutResponse LogoutResponse { get; set; } = new(true, null, null);
        public Task<LocalBrokerAuthorizationResponse> AuthorizeLocalAsync(LocalBrokerAuthorizationRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public async Task<BrokerTokenResponse> ExchangeAsync(BrokerTokenRequest request, string? authorization = null, CancellationToken cancellationToken = default)
        {
            ExchangeCount++;
            return await OnExchange(request);
        }
        public Task<BrokerLogoutResponse> LogoutAsync(BrokerLogoutRequest request, string? authorization = null, CancellationToken cancellationToken = default)
        {
            LogoutRequest = request;
            LogoutAuthorization = authorization;
            return Task.FromResult(LogoutResponse);
        }
    }

    private sealed class FakeAnonymousBackendApiClientProvider(
        IExternalAuthenticationBrokerApi broker,
        Uri? url = null) : IAnonymousBackendApiClientProvider
    {
        public Uri Url { get; } = url ?? new("https://elsa.example/elsa/api/");
        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class => new((T)broker);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://studio.example/", "https://studio.example/");
        public string? LastNavigation { get; private set; }
        protected override void NavigateToCore(string uri, NavigationOptions options) => LastNavigation = ToAbsoluteUri(uri).AbsoluteUri;
    }

    private sealed class BrowserStorageJsRuntime : IJSRuntime
    {
        private readonly Dictionary<string, string> session = new();
        private readonly Dictionary<string, string> local = new();
        public ICollection<string> Operations { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, default, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Operations.Add(identifier);
            if (identifier == "elsaExternalAuthentication.createPkce")
                return new(JsonSerializer.Deserialize<TValue>("{\"state\":\"state-value\",\"codeVerifier\":\"verifier-value\",\"codeChallenge\":\"challenge-value\"}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);

            var storage = identifier.StartsWith("sessionStorage", StringComparison.Ordinal) ? session : local;
            var key = args?[0]?.ToString() ?? string.Empty;
            if (identifier.EndsWith("setItem", StringComparison.Ordinal))
                storage[key] = args?[1]?.ToString() ?? string.Empty;
            else if (identifier.EndsWith("removeItem", StringComparison.Ordinal))
                storage.Remove(key);
            else if (identifier.EndsWith("getItem", StringComparison.Ordinal))
                return new((TValue)(object?)storage.GetValueOrDefault(key)!);

            return new(default(TValue)!);
        }
    }

    private sealed class ThrowingJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new Xunit.Sdk.XunitException($"Unexpected JavaScript call: {identifier}");
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new Xunit.Sdk.XunitException($"Unexpected JavaScript call: {identifier}");
    }
}
