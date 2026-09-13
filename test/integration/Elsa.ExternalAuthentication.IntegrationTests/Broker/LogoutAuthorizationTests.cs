using System.Net;
using System.Net.Http.Json;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TUnit.AspNetCore;

namespace Elsa.ExternalAuthentication.IntegrationTests.Broker;

/// <summary>
/// The upstream-logout continuation is reached by a top-level browser navigation, after the caller's
/// Elsa session has already been revoked. These tests pin that it answers without credentials, and that
/// its sibling <c>Logout</c> still does not.
/// </summary>
public class LogoutAuthorizationTests : WebApplicationTest<LogoutAuthorizationWebApplicationFactory, ExternalAuthenticationTestEntryPoint>
{
    private HttpClient? _client;
    private readonly TestAuthenticationState _authentication = new() { IsAuthenticated = false };
    private readonly IExternalAuthenticationBroker _broker = Substitute.For<IExternalAuthenticationBroker>();
    private readonly ITenantAccessor _tenant = Substitute.For<ITenantAccessor>();

    private HttpClient Client => _client ??= Factory.CreateClient();

    public LogoutAuthorizationTests()
    {
        _broker.ContinueLogoutAsync("handle-a", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(BrokerLogoutResult.Navigate(new Uri("https://idp.example/end-session"))));
        _tenant.TenantId.Returns("tenant-a");
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton(_authentication);
        services.AddSingleton(_broker);
        services.AddSingleton(_tenant);
    }

    [Test]
    public async Task ContinueLogoutRedirectsUpstreamWithoutCredentials()
    {
        var response = await Client.GetAsync("/external-authentication/logout/continue/handle-a");

        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Found);
        await Assert.That(response.Headers.Location?.AbsoluteUri).IsEqualTo("https://idp.example/end-session");
        await _broker.Received(1).ContinueLogoutAsync("handle-a", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ContinueLogoutRejectsAnUnknownHandleWithoutCredentials()
    {
        _broker.ContinueLogoutAsync("unknown", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(BrokerLogoutResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest))));

        var response = await Client.GetAsync("/external-authentication/logout/continue/unknown");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task LogoutStillRequiresAnAuthenticatedCaller()
    {
        var response = await Client.PostAsJsonAsync("/external-authentication/logout", new
        {
            clientId = "studio",
            postLogoutRedirectUri = "https://studio.example/logout-callback",
            mode = "local"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await _broker.DidNotReceive().LogoutAsync(Arg.Any<BrokerLogoutRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
