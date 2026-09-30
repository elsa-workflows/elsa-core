using System.Net;
using Elsa.Http.Resilience;
using Elsa.Resilience.Features;
using Elsa.Resilience.Options;
using Elsa.Resilience.Serialization;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Resilience.IntegrationTests;

/// <summary>
/// The designer lists the resilience strategies to open a definition, and they describe what is configured for the
/// host rather than anything a caller stored, so any signed-in user may read them.
/// </summary>
[Collection(nameof(EndpointSecurityCollection))]
public class ResilienceStrategiesAuthorizationTests : IAsyncLifetime
{
    private const string Path = "/resilience/strategies";
    private WebApplication _app = null!;
    private bool _wasSecurityEnabled;

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = true;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(ResilienceFeature).Assembly];
            options.Filter = endpoint => endpoint.Namespace == "Elsa.Resilience.Endpoints.ResilienceStrategies.List";
        });

        var catalog = Substitute.For<IResilienceStrategyCatalog>();
        catalog.ListAsync(Arg.Any<CancellationToken>()).Returns([new HttpResilienceStrategy()]);
        builder.Services.AddOptions<ResilienceOptions>().Configure(o => o.StrategyTypes.Add(typeof(HttpResilienceStrategy)));
        builder.Services
            .AddSingleton(catalog)
            .AddSingleton<ResilienceStrategySerializer>();

        builder.Services
            .AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme, _ => { });
        builder.Services.AddAuthorization();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();

        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task AuthenticatedUserWithoutAnyPermission_IsAllowed()
    {
        var response = await SendAsync(authenticated: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(nameof(HttpResilienceStrategy), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AnonymousCaller_IsRejected()
    {
        var response = await SendAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendAsync(bool authenticated = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Path);

        if (authenticated)
        {
            // The handler authenticates only when the header is present; a value that parses to no permission grants nothing.
            request.Headers.Add(TestAuthenticationHandler.PermissionHeader, "no-grants");
        }

        return _app.GetTestClient().SendAsync(request);
    }
}
