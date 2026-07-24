using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Features;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Broker;

/// <summary>Contract-level assertions for the anonymous OAuth-shaped broker surface.</summary>
public class BrokerContractTests
{
    [Fact]
    public async Task TokenExchangeRejectsAnUnregisteredPublicOriginBeforeGrantLookup()
    {
        var broker = BrokerSecurityTests.CreateBroker(new BrokerSecurityTests.RecordingAdapter());

        var result = await broker.ExchangeAsync(new BrokerTokenRequest("authorization_code", "studio", new Uri("https://studio.example/authentication/external/callback"), "anything", "verifier", null, "https://attacker.example"));

        Assert.Equal("invalid_request", result.Error?.Error);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task DiscoveryRejectsUnknownAuthenticationClientsWithoutLeakingMethods()
    {
        var broker = BrokerSecurityTests.CreateBroker(new BrokerSecurityTests.RecordingAdapter());

        await Assert.ThrowsAsync<InvalidOperationException>(() => broker.DiscoverAsync("tenant-b", "unknown").AsTask());
    }
}

public class BrokerDiscoveryEndpointContractTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;
    private IExternalAuthenticationBroker _broker = null!;
    private bool _wasSecurityEnabled;

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = false;
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(ExternalAuthenticationFeature).Assembly];
            options.Filter = endpoint => endpoint.Namespace == "Elsa.ExternalAuthentication.Endpoints.Broker";
        });
        _broker = Substitute.For<IExternalAuthenticationBroker>();
        _broker.DiscoverAsync("tenant-a", "studio", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<IReadOnlyCollection<LoginMethod>>([new("local", "local", LoginMethodKind.Local, "Elsa account", "elsa", 0, false, new Uri("/external-authentication/local/authorize", UriKind.Relative))]));
        var tenant = Substitute.For<ITenantAccessor>();
        tenant.TenantId.Returns("tenant-a");
        builder.Services.AddSingleton(_broker);
        builder.Services.AddSingleton(tenant);
        builder.Services.AddRateLimiter(_ => { });
        builder.Services.AddAuthorization();
        _app = builder.Build();
        _app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Elsa.Identity.Constants.CustomClaimTypes.ExternalAuthenticationSessionId, "session-a")], "test"));
            await next(context);
        });
        _app.UseAuthorization();
        _app.UseFastEndpoints();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task LoginMethodsUsesTrustedTenantAndNoStoreContract()
    {
        var response = await _client!.GetAsync("/external-authentication/login-methods?clientId=studio&tenantId=attacker");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Contains("local", body);
        await _broker.Received(1).DiscoverAsync("tenant-a", "studio", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExternalAuthorizeReturnsProviderRedirect()
    {
        _broker.InitiateExternalAsync(Arg.Any<BrokerAuthorizationRequest>(), "tenant-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerInitiationResult.Redirect(new Uri("https://issuer.example/authorize?state=opaque"))));

        var response = await _client!.GetAsync("/external-authentication/authorize/contoso?client_id=studio&redirect_uri=https%3A%2F%2Fstudio.example%2Fcallback&response_type=code&code_challenge=x&code_challenge_method=S256&return_path=%2Fworkflows");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://issuer.example/authorize?state=opaque", response.Headers.Location?.AbsoluteUri);
    }

    [Fact]
    public async Task ProviderCallbackReturnsTrustedClientRedirect()
    {
        _broker.CompleteCallbackAsync("connection-a", "opaque", Arg.Any<IReadOnlyDictionary<string, IReadOnlyCollection<string>>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerCallbackResult.Redirect(new Uri("https://studio.example/callback?code=one"))));

        var response = await _client!.GetAsync("/external-authentication/callback/connection-a?state=opaque&code=provider-code");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://studio.example/callback?code=one", response.Headers.Location?.AbsoluteUri);
    }

    [Fact]
    public async Task LocalAuthorizeReturnsRedirectUriJson()
    {
        _broker.InitiateLocalAsync(Arg.Any<LocalBrokerAuthorizationRequest>(), "tenant-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerCallbackResult.Redirect(new Uri("https://studio.example/callback?code=one"))));

        var response = await _client!.PostAsJsonAsync("/external-authentication/local/authorize", new { clientId = "studio", redirectUri = "https://studio.example/callback", responseType = "code", codeChallenge = "x", codeChallengeMethod = "S256", returnPath = "/workflows", username = "alice", password = "p" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("redirectUri", body);
        Assert.Contains("https://studio.example/callback?code=one", body);
    }

    [Fact]
    public async Task TokenFormExchangeReturnsTokenShape()
    {
        _broker.ExchangeAsync(Arg.Any<BrokerTokenRequest>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerTokenResult.Success(new ExternalTokenResponse("access", "Bearer", 3600, "refresh", 7200, 28800))));

        var response = await _client!.PostAsync("/external-authentication/token", new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "authorization_code", ["client_id"] = "studio", ["redirect_uri"] = "https://studio.example/callback", ["code"] = "code", ["code_verifier"] = "verifier" }));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("accessToken", body);
        Assert.Contains("refreshToken", body);
    }

    [Fact]
    public async Task LogoutAndLogoutCallbackHonorBrokerResponses()
    {
        _broker.LogoutAsync(Arg.Any<BrokerLogoutRequest>(), "session-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerLogoutResult.Complete(new Uri("https://studio.example/logout-callback"))));
        _broker.CompleteLogoutAsync("connection-a", "opaque", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerCallbackResult.Redirect(new Uri("https://studio.example/logout-callback"))));

        var logout = await _client!.PostAsJsonAsync("/external-authentication/logout", new { clientId = "studio", postLogoutRedirectUri = "https://studio.example/logout-callback", mode = "local" });
        var callback = await _client!.GetAsync("/external-authentication/logout/callback/connection-a?state=opaque");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Contains("completed", await logout.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Found, callback.StatusCode);
        Assert.Equal("https://studio.example/logout-callback", callback.Headers.Location?.AbsoluteUri);
    }
}
