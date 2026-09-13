using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using TUnit.AspNetCore;

namespace Elsa.ExternalAuthentication.IntegrationTests.Broker;

/// <summary>Contract-level assertions for the anonymous OAuth-shaped broker surface.</summary>
public class BrokerContractTests
{
    [Test]
    public async Task TokenExchangeRejectsAnUnregisteredPublicOriginBeforeGrantLookup()
    {
        var broker = BrokerSecurityTests.CreateBroker(new BrokerSecurityTests.RecordingAdapter());

        var result = await broker.ExchangeAsync(new BrokerTokenRequest("authorization_code", "studio", new Uri("https://studio.example/authentication/external/callback"), "anything", "verifier", null, "https://attacker.example"));

        await Assert.That(result.Error?.Error).IsEqualTo("invalid_request");
        await Assert.That(result.Token).IsNull();
    }

    [Test]
    public async Task DiscoveryRejectsUnknownAuthenticationClientsWithoutLeakingMethods()
    {
        var broker = BrokerSecurityTests.CreateBroker(new BrokerSecurityTests.RecordingAdapter());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => broker.DiscoverAsync("tenant-b", "unknown").AsTask());
    }

    [Test]
    public async Task DiscoveryDoesNotAdvertiseAnInvalidConnection()
    {
        var broker = BrokerSecurityTests.CreateBroker(
            new BrokerSecurityTests.RecordingAdapter(),
            connectionValidity: ConnectionValidity.Unknown,
            assessedValidity: ConnectionValidity.Invalid,
            includeLoginMethod: true);

        var methods = await broker.DiscoverAsync("tenant-a", "studio");

        await Assert.That(methods).DoesNotContain(method => method.Id == "connection-a");
    }

    [Test]
    public async Task InitiationRejectsAConnectionThatFailsRuntimeValidityAssessment()
    {
        var adapter = new BrokerSecurityTests.RecordingAdapter();
        var broker = BrokerSecurityTests.CreateBroker(
            adapter,
            connectionValidity: ConnectionValidity.Unknown,
            assessedValidity: ConnectionValidity.Invalid);

        var result = await broker.InitiateExternalAsync(new BrokerAuthorizationRequest(
            "studio",
            new Uri("https://studio.example/authentication/external/callback"),
            "code",
            "challenge",
            "S256",
            "/workflows",
            "contoso"), "tenant-a");

        await Assert.That(result.Error?.Error).IsEqualTo("method_unavailable");
        await Assert.That(adapter.Connection).IsNull();
    }
}

public class BrokerDiscoveryEndpointContractTests : WebApplicationTest<BrokerDiscoveryEndpointContractWebApplicationFactory, ExternalAuthenticationTestEntryPoint>
{
    private HttpClient? _client;
    private readonly TestAuthenticationState _authentication = new();
    private readonly IExternalAuthenticationBroker _broker = Substitute.For<IExternalAuthenticationBroker>();
    private readonly ITenantAccessor _tenant = Substitute.For<ITenantAccessor>();

    private HttpClient Client => _client ??= Factory.CreateClient();

    public BrokerDiscoveryEndpointContractTests()
    {
        _authentication.SetClaims(new Claim(Elsa.Identity.Constants.CustomClaimTypes.ExternalAuthenticationSessionId, "session-a"));
        _broker.DiscoverAsync("tenant-a", "studio", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<IReadOnlyCollection<LoginMethod>>([new("local", "local", LoginMethodKind.Local, "Elsa account", "elsa", 0, false, new Uri("/external-authentication/local/authorize", UriKind.Relative))]));
        _tenant.TenantId.Returns("tenant-a");
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton(_authentication);
        services.AddSingleton(_broker);
        services.AddSingleton(_tenant);
    }

    [Test]
    public async Task LoginMethodsUsesTrustedTenantAndNoStoreContract()
    {
        var response = await Client.GetAsync("/external-authentication/login-methods?clientId=studio&tenantId=attacker");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.CacheControl?.ToString()).IsEqualTo("no-store");
        await Assert.That(body).Contains("local");
        await _broker.Received(1).DiscoverAsync("tenant-a", "studio", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExternalAuthorizeReturnsProviderRedirect()
    {
        _broker.InitiateExternalAsync(Arg.Any<BrokerAuthorizationRequest>(), "tenant-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerInitiationResult.Redirect(new Uri("https://issuer.example/authorize?state=opaque"))));

        var response = await Client.GetAsync("/external-authentication/authorize/contoso?client_id=studio&redirect_uri=https%3A%2F%2Fstudio.example%2Fcallback&response_type=code&code_challenge=x&code_challenge_method=S256&return_path=%2Fworkflows");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Found);
        await Assert.That(response.Headers.Location?.AbsoluteUri).IsEqualTo("https://issuer.example/authorize?state=opaque");
    }

    [Test]
    public async Task ProviderCallbackReturnsTrustedClientRedirect()
    {
        _broker.CompleteCallbackAsync("contoso", "opaque", Arg.Any<IReadOnlyDictionary<string, IReadOnlyCollection<string>>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerCallbackResult.Redirect(new Uri("https://studio.example/callback?code=one"))));

        var response = await Client.GetAsync("/external-authentication/callback/contoso?state=opaque&code=provider-code");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Found);
        await Assert.That(response.Headers.Location?.AbsoluteUri).IsEqualTo("https://studio.example/callback?code=one");
    }

    [Test]
    public async Task LocalAuthorizeReturnsRedirectUriJson()
    {
        _broker.InitiateLocalAsync(Arg.Any<LocalBrokerAuthorizationRequest>(), "tenant-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerCallbackResult.Redirect(new Uri("https://studio.example/callback?code=one"))));

        var response = await Client.PostAsJsonAsync("/external-authentication/local/authorize", new { clientId = "studio", redirectUri = "https://studio.example/callback", responseType = "code", codeChallenge = "x", codeChallengeMethod = "S256", returnPath = "/workflows", username = "alice", password = "p" });
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).Contains("redirectUri");
        await Assert.That(body).Contains("https://studio.example/callback?code=one");
    }

    [Test]
    public async Task TokenFormExchangeReturnsTokenShape()
    {
        _broker.ExchangeAsync(Arg.Any<BrokerTokenRequest>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerTokenResult.Success(new ExternalTokenResponse("access", "Bearer", 3600, "refresh", 7200, 28800))));

        var response = await Client.PostAsync("/external-authentication/token", new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "authorization_code", ["client_id"] = "studio", ["redirect_uri"] = "https://studio.example/callback", ["code"] = "code", ["code_verifier"] = "verifier" }));
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).Contains("accessToken");
        await Assert.That(body).Contains("refreshToken");
    }

    [Test]
    public async Task LogoutAndLogoutCallbackHonorBrokerResponses()
    {
        _broker.LogoutAsync(Arg.Any<BrokerLogoutRequest>(), "session-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerLogoutResult.Complete(new Uri("https://studio.example/logout-callback"))));
        _broker.CompleteLogoutAsync("contoso", "opaque", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(BrokerCallbackResult.Redirect(new Uri("https://studio.example/logout-callback"))));

        var logout = await Client.PostAsJsonAsync("/external-authentication/logout", new { clientId = "studio", postLogoutRedirectUri = "https://studio.example/logout-callback", mode = "local" });
        var callback = await Client.GetAsync("/external-authentication/logout/callback/contoso?state=opaque");

        await Assert.That(logout.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await logout.Content.ReadAsStringAsync()).Contains("completed");
        await Assert.That(callback.StatusCode).IsEqualTo(HttpStatusCode.Found);
        await Assert.That(callback.Headers.Location?.AbsoluteUri).IsEqualTo("https://studio.example/logout-callback");
    }
}
