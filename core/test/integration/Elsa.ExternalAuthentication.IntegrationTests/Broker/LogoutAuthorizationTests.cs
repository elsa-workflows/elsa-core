using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Features;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Broker;

/// <summary>
/// The upstream-logout continuation is reached by a top-level browser navigation, after the caller's
/// Elsa session has already been revoked. These tests pin that it answers without credentials, and that
/// its sibling <c>Logout</c> still does not.
/// </summary>
[Collection(nameof(EndpointSecurityCollection))]
public class LogoutAuthorizationTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;
    private IExternalAuthenticationBroker _broker = null!;
    private bool _wasSecurityEnabled;

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = true;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(ExternalAuthenticationFeature).Assembly];
            options.Filter = endpoint => endpoint.Namespace == "Elsa.ExternalAuthentication.Endpoints.Broker";
        });

        _broker = Substitute.For<IExternalAuthenticationBroker>();
        _broker.ContinueLogoutAsync("handle-a", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(BrokerLogoutResult.Navigate(new Uri("https://idp.example/end-session"))));

        var tenant = Substitute.For<ITenantAccessor>();
        tenant.TenantId.Returns("tenant-a");
        builder.Services.AddSingleton(_broker);
        builder.Services.AddSingleton(tenant);
        builder.Services.AddRateLimiter(_ => { });

        // A scheme that never authenticates, standing in for a browser navigation that carries no
        // Authorization header. No principal is injected: that is the condition under test.
        builder.Services
            .AddAuthentication(NoCredentialsHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, NoCredentialsHandler>(NoCredentialsHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        _app = builder.Build();
        _app.UseAuthentication();
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
    public async Task ContinueLogoutRedirectsUpstreamWithoutCredentials()
    {
        var response = await _client!.GetAsync("/external-authentication/logout/continue/handle-a");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://idp.example/end-session", response.Headers.Location?.AbsoluteUri);
        await _broker.Received(1).ContinueLogoutAsync("handle-a", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ContinueLogoutRejectsAnUnknownHandleWithoutCredentials()
    {
        _broker.ContinueLogoutAsync("unknown", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(BrokerLogoutResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest))));

        var response = await _client!.GetAsync("/external-authentication/logout/continue/unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LogoutStillRequiresAnAuthenticatedCaller()
    {
        var response = await _client!.PostAsJsonAsync("/external-authentication/logout", new
        {
            clientId = "studio",
            postLogoutRedirectUri = "https://studio.example/logout-callback",
            mode = "local"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _broker.DidNotReceive().LogoutAsync(Arg.Any<BrokerLogoutRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private sealed class NoCredentialsHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "NoCredentials";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
    }
}
