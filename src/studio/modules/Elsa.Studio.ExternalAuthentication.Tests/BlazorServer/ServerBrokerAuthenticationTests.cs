using System.Net;
using System.Security.Claims;
using Elsa.Studio.ExternalAuthentication.BlazorServer.Extensions;
using Elsa.Studio.ExternalAuthentication.BlazorServer.Services;
using Elsa.Studio.ExternalAuthentication.BlazorServer.Controllers;
using Elsa.Studio.ExternalAuthentication.BlazorServer.HttpMessageHandlers;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.BlazorServer;

public sealed class ServerBrokerAuthenticationTests
{
    [Fact]
    public void ServerCoordinator_ExposesLocalLoginPostTargetThroughInterface()
    {
        IExternalAuthenticationLoginCoordinator coordinator = new ServerExternalAuthenticationLoginCoordinator(
            new UnusedAnonymousBackendApiClientProvider(),
            new ExternalAuthenticationClientOptions(),
            new TestNavigationManager());

        Assert.Equal("/authentication/external/local-login", coordinator.LocalLoginAction);
    }

    [Fact]
    public void BrokerRegistration_UsesHttpOnlySecureCookieAndServerTicketStore()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthenticationBroker(options =>
        {
            options.ClientId = "studio-server";
            options.ClientSecret = "server-secret";
        });
        using var provider = services.BuildServiceProvider();

        var cookie = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(ServerExternalAuthenticationStateProvider.Scheme);

        Assert.Equal("ElsaStudio.ExternalAuthentication", cookie.Cookie.Name);
        Assert.True(cookie.Cookie.HttpOnly);
        Assert.Equal(Microsoft.AspNetCore.Http.CookieSecurePolicy.Always, cookie.Cookie.SecurePolicy);
        Assert.Equal(Microsoft.AspNetCore.Http.SameSiteMode.Lax, cookie.Cookie.SameSite);
        Assert.IsType<ServerExternalAuthenticationTicketStore>(cookie.SessionStore);
    }

    [Fact]
    public async Task TicketStore_RetainsRefreshTokenOnlyInTheServerTicket()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new ServerExternalAuthenticationTicketStore(cache);
        var properties = new AuthenticationProperties();
        properties.StoreTokens([new AuthenticationToken { Name = "refresh_token", Value = "never-in-browser" }]);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity("test")), properties, ServerExternalAuthenticationStateProvider.Scheme);

        var key = await store.StoreAsync(ticket);
        var retrieved = await store.RetrieveAsync(key);

        Assert.NotEqual("never-in-browser", key);
        Assert.Equal("never-in-browser", retrieved!.Properties.GetTokenValue("refresh_token"));
    }

    [Fact]
    public void ServerMode_RejectsAClientWithoutConfidentialSecret()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddExternalAuthenticationBroker(options => options.ClientId = "studio-server"));

        Assert.Contains("confidential", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ServerMode_RejectsCustomCallbackPaths()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddExternalAuthenticationBroker(options =>
        {
            options.ClientId = "studio-server";
            options.ClientSecret = "secret";
            options.CallbackPath = "/custom-callback";
        }));

        Assert.Contains("fixed Studio callback paths", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalLogin_RequiresAnAntiforgeryToken()
    {
        var method = typeof(ExternalAuthenticationController).GetMethod(nameof(ExternalAuthenticationController.LocalLogin));

        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes(inherit: true), attribute => attribute.GetType().Name == "ValidateAntiForgeryTokenAttribute");
    }

    [Fact]
    public async Task LocalLogin_WhenTheBrokerRejectsCredentials_RedirectsToChooserWithASafeErrorOutcome()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("studio.example.test");
        var options = new ExternalAuthenticationClientOptions { ClientId = "studio-server", ClientSecret = "secret" };
        var anonymous = new FakeAnonymousBackendApiClientProvider(new FakeBrokerApi(throwsOnLocalAuthorization: true));
        var logger = new RecordingLogger<ExternalAuthenticationController>();
        var stateProvider = new ServerExternalAuthenticationStateProvider(
            new HttpContextAccessor { HttpContext = context },
            anonymous,
            new ServerExternalAuthenticationRefreshCoordinator(),
            options);
        var controller = CreateController(
            context,
            anonymous,
            new FakeTransactionStore(new("state", "verifier", "/workflows", DateTimeOffset.UtcNow.AddMinutes(1))),
            stateProvider,
            options,
            logger);

        var result = await controller.LocalLogin("admin", "wrong-password", "/workflows", CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("choose=true", redirect.Url);
        Assert.Contains("returnPath=%2Fworkflows", redirect.Url);
        Assert.Contains("error=sign_in_failed", redirect.Url);
        var log = Assert.Single(logger.Entries, entry => entry.EventId.Name == "LocalAuthorizationFailed");
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.IsType<HttpRequestException>(log.Exception);
        Assert.DoesNotContain("admin", log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("wrong-password", log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PrincipalFactory_PreservesElsaPermissionClaims()
    {
        const string token = "header.eyJzdWIiOiIxIiwicGVybWlzc2lvbnMiOlsid29ya2Zsb3dzOnJlYWQiXX0.signature";

        var principal = ServerExternalAuthenticationStateProvider.CreatePrincipal(token);

        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.Contains(principal.FindAll("permissions"), claim => claim.Value == "workflows:read");
    }

    [Fact]
    public void MalformedBrokerToken_IsRejectedBeforeAnAuthenticatedCookieIsCreated()
    {
        Assert.Throws<InvalidOperationException>(() => ServerExternalAuthenticationStateProvider.CreatePrincipal("not-a-jwt"));
    }

    [Fact]
    public void TicketLifetime_IsBoundedByRefreshAndExternalSession_NotAccessToken()
    {
        var now = new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);
        var properties = ServerExternalAuthenticationStateProvider.CreateAuthenticationProperties(
            new("token", "Bearer", 300, "refresh", 3600, 1800), now);

        Assert.Equal(now.AddMinutes(30), properties.ExpiresUtc);
        Assert.Equal(now.AddMinutes(5).ToString("O"), properties.GetTokenValue("access_expires_at"));
        Assert.Equal("refresh", properties.GetTokenValue("refresh_token"));
    }

    private sealed class UnusedAnonymousBackendApiClientProvider : IAnonymousBackendApiClientProvider
    {
        public Uri Url => new("https://backend.example");
        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://studio.example/", "https://studio.example/");
        protected override void NavigateToCore(string uri, NavigationOptions options) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData("state", true)]
    [InlineData(null, true)]
    [InlineData("wrong-state", false)]
    public async Task ProviderError_WithTrustedTransaction_ReturnsToTheTrustedChooserPath(string? callbackState, bool isTrustedCallback)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("studio.example.test");
        context.Request.QueryString = new QueryString("?correlation_id=broker-123");
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new ExternalAuthenticationClientOptions { ClientId = "studio-server", ClientSecret = "secret" };
        var anonymous = new FakeAnonymousBackendApiClientProvider();
        var stateProvider = new ServerExternalAuthenticationStateProvider(accessor, anonymous, new ServerExternalAuthenticationRefreshCoordinator(), options);
        var transactionStore = new FakeTransactionStore(new("state", "verifier", "/workflows", DateTimeOffset.UtcNow.AddMinutes(1)));
        var logger = new RecordingLogger<ExternalAuthenticationController>();
        var controller = CreateController(context, anonymous, transactionStore, stateProvider, options, logger);

        var result = await controller.Callback(code: null, state: callbackState, error: "access_denied", CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        if (!isTrustedCallback)
        {
            Assert.Equal("/login?choose=true&returnPath=%2F", redirect.Url);
            Assert.Contains(logger.Entries, entry => entry.EventId.Name == "CallbackStateRejected" && entry.Level == LogLevel.Warning);
            return;
        }

        Assert.Contains("choose=true", redirect.Url);
        Assert.Contains("returnPath=%2Fworkflows", redirect.Url);
        Assert.Contains("error=external_sign_in_failed", redirect.Url);
        var log = Assert.Single(logger.Entries, entry => entry.EventId.Name == "CallbackBrokerFailure");
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Contains("broker-123", log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("access_denied", log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Callback_WhenBrokerExchangeFails_LogsTheFailureWithoutCallbackSecrets()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("studio.example.test");
        context.Request.QueryString = new QueryString("?correlation_id=broker-123");
        var options = new ExternalAuthenticationClientOptions { ClientId = "studio-server", ClientSecret = "secret" };
        var anonymous = new FakeAnonymousBackendApiClientProvider(new FakeBrokerApi(throwsOnExchange: true));
        var stateProvider = new ServerExternalAuthenticationStateProvider(
            new HttpContextAccessor { HttpContext = context },
            anonymous,
            new ServerExternalAuthenticationRefreshCoordinator(),
            options);
        var transactionStore = new FakeTransactionStore(
            new("sensitive-state", "sensitive-verifier", "/workflows", DateTimeOffset.UtcNow.AddMinutes(1)));
        var logger = new RecordingLogger<ExternalAuthenticationController>();
        var controller = CreateController(context, anonymous, transactionStore, stateProvider, options, logger);

        var result = await controller.Callback("sensitive-completion-code", "sensitive-state", error: null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=external_sign_in_failed", redirect.Url);
        var log = Assert.Single(logger.Entries, entry => entry.EventId.Name == "CallbackCompletionFailed");
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.IsType<HttpRequestException>(log.Exception);
        Assert.Contains("broker-123", log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-completion-code", log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-state", log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-verifier", log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LocalLoginCallbackError_ReturnsToChooserWithASafeErrorOutcome()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("studio.example.test");
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new ExternalAuthenticationClientOptions { ClientId = "studio-server", ClientSecret = "secret" };
        var anonymous = new FakeAnonymousBackendApiClientProvider();
        var stateProvider = new ServerExternalAuthenticationStateProvider(
            accessor,
            anonymous,
            new ServerExternalAuthenticationRefreshCoordinator(),
            options);
        var transactionStore = new FakeTransactionStore(
            new("state", "verifier", "/workflows", DateTimeOffset.UtcNow.AddMinutes(1), "local-sign-in"));
        var controller = CreateController(context, anonymous, transactionStore, stateProvider, options);

        var result = await controller.Callback(code: null, state: "state", error: "access_denied", CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("choose=true", redirect.Url);
        Assert.Contains("returnPath=%2Fworkflows", redirect.Url);
        Assert.Contains("error=sign_in_failed", redirect.Url);
    }

    [Fact]
    public async Task Callback_UsesConfidentialBasicExchangeAndExactState()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(ServerExternalAuthenticationStateProvider.Scheme)
            .AddCookie(ServerExternalAuthenticationStateProvider.Scheme);
        using var serviceProvider = services.BuildServiceProvider();
        using var callbackScope = serviceProvider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = callbackScope.ServiceProvider };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("studio.example.test");
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new ExternalAuthenticationClientOptions { ClientId = "studio-server", ClientSecret = "secret" };
        var broker = new FakeBrokerApi();
        var anonymous = new FakeAnonymousBackendApiClientProvider(broker);
        var stateProvider = new ServerExternalAuthenticationStateProvider(accessor, anonymous, new ServerExternalAuthenticationRefreshCoordinator(), options);
        var controller = CreateController(context, anonymous, new FakeTransactionStore(new("state", "verifier", "/workflows", DateTimeOffset.UtcNow.AddMinutes(1))), stateProvider, options);

        var result = await controller.Callback("completion-code", "state", null, CancellationToken.None);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/workflows", redirect.Url);
        Assert.Equal("authorization_code", broker.ExchangeRequest!.GrantType);
        Assert.Equal("completion-code", broker.ExchangeRequest.Code);
        Assert.Equal("verifier", broker.ExchangeRequest.CodeVerifier);
        Assert.Equal("Basic c3R1ZGlvLXNlcnZlcjpzZWNyZXQ=", broker.ExchangeAuthorization);

        var setCookie = Assert.Single(context.Response.Headers.SetCookie);
        var cookie = setCookie!.Split(';', 2)[0];
        using var redirectedScope = serviceProvider.CreateScope();
        var redirectedContext = new DefaultHttpContext { RequestServices = redirectedScope.ServiceProvider };
        redirectedContext.Request.Scheme = "https";
        redirectedContext.Request.Host = new HostString("studio.example.test");
        redirectedContext.Request.Headers.Cookie = cookie;

        var authentication = await redirectedContext.AuthenticateAsync(ServerExternalAuthenticationStateProvider.Scheme);

        Assert.True(authentication.Succeeded, authentication.Failure?.ToString() ?? "Authentication returned no result.");
        Assert.True(authentication.Principal!.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task Refresh_RotatesAndReplacesTheServerHeldRefreshToken()
    {
        var ticket = CreateExpiredTicket();
        var authentication = new RecordingAuthenticationService(ticket);
        var context = CreateAuthenticatedContext(ticket.Principal, authentication);
        var accessor = new HttpContextAccessor { HttpContext = context };
        var broker = new FakeBrokerApi();
        var provider = new ServerExternalAuthenticationStateProvider(accessor, new FakeAnonymousBackendApiClientProvider(broker), new ServerExternalAuthenticationRefreshCoordinator(), Options());

        var accessToken = await provider.GetAccessTokenAsync();

        Assert.Equal("header.eyJzdWIiOiIxIiwicGVybWlzc2lvbnMiOlsid29ya2Zsb3dzOnJlYWQiXX0.signature", accessToken);
        Assert.Equal("old-refresh", broker.ExchangeRequest!.RefreshToken);
        Assert.Equal("refresh", authentication.SignedInProperties!.GetTokenValue("refresh_token"));
        Assert.False(authentication.SignedOut);
    }

    [Fact]
    public async Task ConcurrentRefresh_ExchangesTheOldCredentialOnlyOnce()
    {
        var broker = new FakeBrokerApi(delay: TimeSpan.FromMilliseconds(50));
        var coordinator = new ServerExternalAuthenticationRefreshCoordinator();
        var firstTicket = CreateExpiredTicket();
        var secondTicket = CreateExpiredTicket();
        var first = new ServerExternalAuthenticationStateProvider(new HttpContextAccessor { HttpContext = CreateAuthenticatedContext(firstTicket.Principal, new RecordingAuthenticationService(firstTicket)) }, new FakeAnonymousBackendApiClientProvider(broker), coordinator, Options());
        var second = new ServerExternalAuthenticationStateProvider(new HttpContextAccessor { HttpContext = CreateAuthenticatedContext(secondTicket.Principal, new RecordingAuthenticationService(secondTicket)) }, new FakeAnonymousBackendApiClientProvider(broker), coordinator, Options());

        await Task.WhenAll(first.GetAccessTokenAsync(), second.GetAccessTokenAsync());

        Assert.Equal(1, broker.ExchangeCalls);
    }

    [Fact]
    public async Task RefreshFailure_SignsOutTheServerSession()
    {
        var ticket = CreateExpiredTicket();
        var authentication = new RecordingAuthenticationService(ticket);
        var context = CreateAuthenticatedContext(ticket.Principal, authentication);
        var provider = new ServerExternalAuthenticationStateProvider(new HttpContextAccessor { HttpContext = context }, new FakeAnonymousBackendApiClientProvider(new FakeBrokerApi(throwsOnExchange: true)), new ServerExternalAuthenticationRefreshCoordinator(), Options());

        Assert.Null(await provider.GetAccessTokenAsync());
        Assert.True(authentication.SignedOut);
    }

    [Fact]
    public async Task AuthenticatedApiHandler_AttachesTheRefreshedElsaAccessToken()
    {
        var handler = new ExternalAuthenticationAuthenticatingApiHttpMessageHandler(new StaticBlazorServiceAccessor(new ServiceCollection()
            .AddSingleton<IExternalAuthenticationTokenProvider>(new StaticTokenProvider("refreshed-access-token"))
            .BuildServiceProvider()))
        {
            InnerHandler = new CapturingHandler()
        };
        using var client = new HttpClient(handler);

        await client.GetAsync("https://elsa.example.test/elsa/api/workflows");

        var captured = Assert.IsType<CapturingHandler>(handler.InnerHandler);
        Assert.Equal("Bearer", captured.Authorization!.Scheme);
        Assert.Equal("refreshed-access-token", captured.Authorization.Parameter);
    }

    [Fact]
    public async Task UpstreamLogout_PreservesBackendPathAndProtectsTheReturnPath()
    {
        var ticket = CreateCurrentTicket();
        var authentication = new RecordingAuthenticationService(ticket);
        var context = CreateAuthenticatedContext(ticket.Principal, authentication);
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("studio.example.test");
        var broker = new FakeBrokerApi { LogoutResult = new(false, "/external-authentication/logout/continue/opaque", null) };
        var anonymous = new FakeAnonymousBackendApiClientProvider(broker);
        var transactions = new FakeTransactionStore(new("unused", "", "/", DateTimeOffset.UtcNow.AddMinutes(1)));
        var state = new ServerExternalAuthenticationStateProvider(new HttpContextAccessor { HttpContext = context }, anonymous, new ServerExternalAuthenticationRefreshCoordinator(), Options());
        var controller = CreateController(context, anonymous, transactions, state, Options());

        var result = await controller.Logout("upstream", "/workflows", CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://elsa.example.test/elsa/api/external-authentication/logout/continue/opaque", redirect.Url);
        Assert.Equal("logout", transactions.Stored!.Purpose);
        Assert.Equal("/workflows", transactions.Stored.ReturnPath);

        var callbackContext = new DefaultHttpContext();
        var callback = CreateController(callbackContext, anonymous, transactions, state, Options());
        var callbackResult = callback.LogoutCallback();

        Assert.Equal("/workflows", Assert.IsType<LocalRedirectResult>(callbackResult).Url);
    }

    [Fact]
    public async Task UpstreamLogout_RejectsProviderOriginNavigation()
    {
        var ticket = CreateCurrentTicket();
        var context = CreateAuthenticatedContext(ticket.Principal, new RecordingAuthenticationService(ticket));
        var broker = new FakeBrokerApi { LogoutResult = new(false, "https://provider.example.test/logout", null) };
        var anonymous = new FakeAnonymousBackendApiClientProvider(broker);
        var transactions = new FakeTransactionStore(new("unused", "", "/", DateTimeOffset.UtcNow.AddMinutes(1)));
        var state = new ServerExternalAuthenticationStateProvider(new HttpContextAccessor { HttpContext = context }, anonymous, new ServerExternalAuthenticationRefreshCoordinator(), Options());

        var result = await CreateController(context, anonymous, transactions, state, Options()).Logout("upstream", "/workflows", CancellationToken.None);

        Assert.Equal("/workflows", Assert.IsType<LocalRedirectResult>(result).Url);
        Assert.Null(transactions.Stored);
    }

    [Fact]
    public void Login_PreservesBackendPathWhenBaseUrlHasNoTrailingSlash()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("studio.example.test");
        var options = Options();
        var anonymous = new FakeAnonymousBackendApiClientProvider(
            url: new Uri("https://elsa.example.test/elsa/api"));
        var state = new ServerExternalAuthenticationStateProvider(
            new HttpContextAccessor { HttpContext = context },
            anonymous,
            new ServerExternalAuthenticationRefreshCoordinator(),
            options);
        var controller = CreateController(
            context,
            anonymous,
            new FakeTransactionStore(new("unused", "", "/", DateTimeOffset.UtcNow.AddMinutes(1))),
            state,
            options);

        var result = controller.Login("keycloak-workforce", "/");

        var redirect = Assert.IsType<RedirectResult>(result);
        var redirectUri = new Uri(redirect.Url!);
        Assert.Equal(
            "/elsa/api/external-authentication/authorize/keycloak-workforce",
            redirectUri.AbsolutePath);
    }

    private static ExternalAuthenticationClientOptions Options() => new() { ClientId = "studio-server", ClientSecret = "secret" };

    private static AuthenticationTicket CreateExpiredTicket()
    {
        var principal = ServerExternalAuthenticationStateProvider.CreatePrincipal("header.eyJzdWIiOiIxIiwicGVybWlzc2lvbnMiOlsid29ya2Zsb3dzOnJlYWQiXX0.signature");
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "old-access" },
            new AuthenticationToken { Name = "refresh_token", Value = "old-refresh" },
            new AuthenticationToken { Name = "access_expires_at", Value = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O") }
        ]);
        return new AuthenticationTicket(principal, properties, ServerExternalAuthenticationStateProvider.Scheme);
    }

    private static AuthenticationTicket CreateCurrentTicket()
    {
        var ticket = CreateExpiredTicket();
        ticket.Properties.UpdateTokenValue("access_token", "header.eyJzdWIiOiIxIiwicGVybWlzc2lvbnMiOlsid29ya2Zsb3dzOnJlYWQiXX0.signature");
        ticket.Properties.UpdateTokenValue("access_expires_at", DateTimeOffset.UtcNow.AddMinutes(30).ToString("O"));
        return ticket;
    }

    private static HttpContext CreateAuthenticatedContext(ClaimsPrincipal principal, IAuthenticationService authentication)
    {
        var services = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        return new DefaultHttpContext { User = principal, RequestServices = services };
    }

    private static ExternalAuthenticationController CreateController(
        HttpContext context,
        IAnonymousBackendApiClientProvider anonymous,
        IServerExternalAuthenticationTransactionStore transactionStore,
        ServerExternalAuthenticationStateProvider stateProvider,
        ExternalAuthenticationClientOptions options,
        ILogger<ExternalAuthenticationController>? logger = null) => new(anonymous, transactionStore, stateProvider, options, logger ?? NullLogger<ExternalAuthenticationController>.Instance)
    {
        ControllerContext = new ControllerContext { HttpContext = context }
    };

    private sealed record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add(new(logLevel, eventId, formatter(state, exception), exception));
    }

    private sealed class FakeTransactionStore(ServerExternalAuthenticationTransaction transaction) : IServerExternalAuthenticationTransactionStore
    {
        public ServerExternalAuthenticationTransaction? Stored { get; private set; }
        public void Store(HttpResponse response, ServerExternalAuthenticationTransaction value) => Stored = value;
        public bool TryTake(HttpRequest request, HttpResponse response, out ServerExternalAuthenticationTransaction value)
        {
            value = Stored ?? transaction;
            return true;
        }
    }

    private sealed class FakeAnonymousBackendApiClientProvider(
        IExternalAuthenticationBrokerApi? broker = null,
        Uri? url = null) : IAnonymousBackendApiClientProvider
    {
        public Uri Url { get; } = url ?? new("https://elsa.example.test/elsa/api/");
        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            ValueTask.FromResult((T)(object)(broker ?? new FakeBrokerApi()));
    }

    private sealed class FakeBrokerApi(
        TimeSpan? delay = null,
        bool throwsOnExchange = false,
        bool throwsOnLocalAuthorization = false) : IExternalAuthenticationBrokerApi
    {
        public BrokerTokenRequest? ExchangeRequest { get; private set; }
        public string? ExchangeAuthorization { get; private set; }
        public int ExchangeCalls { get; private set; }
        public BrokerLogoutResponse? LogoutResult { get; init; }
        public Task<LocalBrokerAuthorizationResponse> AuthorizeLocalAsync(LocalBrokerAuthorizationRequest request, CancellationToken cancellationToken = default) =>
            throwsOnLocalAuthorization
                ? Task.FromException<LocalBrokerAuthorizationResponse>(new HttpRequestException())
                : Task.FromException<LocalBrokerAuthorizationResponse>(new NotSupportedException());
        public async Task<BrokerTokenResponse> ExchangeAsync(BrokerTokenRequest request, string? authorization = null, CancellationToken cancellationToken = default)
        {
            ExchangeRequest = request;
            ExchangeAuthorization = authorization;
            ExchangeCalls++;
            if (delay is not null)
                await Task.Delay(delay.Value, cancellationToken);
            if (throwsOnExchange)
                throw new HttpRequestException();
            return new BrokerTokenResponse("header.eyJzdWIiOiIxIiwicGVybWlzc2lvbnMiOlsid29ya2Zsb3dzOnJlYWQiXX0.signature", "Bearer", 300, "refresh", 3600, 1800);
        }
        public Task<BrokerLogoutResponse> LogoutAsync(BrokerLogoutRequest request, string? authorization = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(LogoutResult ?? throw new NotSupportedException());
    }

    private sealed class RecordingAuthenticationService(AuthenticationTicket ticket) : IAuthenticationService
    {
        public AuthenticationProperties? SignedInProperties { get; private set; }
        public bool SignedOut { get; private set; }
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.Success(ticket));
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) { SignedInProperties = properties; return Task.CompletedTask; }
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) { SignedOut = true; return Task.CompletedTask; }
    }

    private sealed class StaticTokenProvider(string token) : IExternalAuthenticationTokenProvider
    {
        public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(token);
    }

    private sealed class StaticBlazorServiceAccessor(IServiceProvider services) : IBlazorServiceAccessor
    {
        public IServiceProvider Services { get; set; } = services;
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public System.Net.Http.Headers.AuthenticationHeaderValue? Authorization { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
