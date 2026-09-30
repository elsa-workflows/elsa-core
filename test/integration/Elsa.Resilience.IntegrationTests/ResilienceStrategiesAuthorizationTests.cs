using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa.Http.Resilience;
using Elsa.Resilience.Features;
using Elsa.Resilience.Options;
using Elsa.Resilience.Serialization;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        catalog.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
        builder.Services.AddOptions<ResilienceOptions>().Configure(o => o.StrategyTypes.Add(typeof(HttpResilienceStrategy)));
        builder.Services
            .AddSingleton(catalog)
            .AddSingleton<ResilienceStrategySerializer>();

        builder.Services
            .AddAuthentication(PermissionHeaderAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, PermissionHeaderAuthenticationHandler>(PermissionHeaderAuthenticationHandler.SchemeName, _ => { });
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
    public async Task UserWhoCanOnlyViewDefinitions_IsAllowed()
    {
        var response = await SendAsync("workflows/definitions:view");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousCaller_IsRejected()
    {
        var response = await SendAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendAsync(string? permissions = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Path);

        if (permissions != null)
        {
            request.Headers.Add(PermissionHeaderAuthenticationHandler.HeaderName, permissions);
        }

        return _app.GetTestClient().SendAsync(request);
    }

    /// <summary>
    /// Authenticates a caller holding the permissions named in a header; a request without the header stays anonymous.
    /// </summary>
    private sealed class PermissionHeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Header";
        public const string HeaderName = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var permissions))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = permissions.ToString().Split(',').Select(x => new Claim(PermissionNames.ClaimType, x));
            var identity = new ClaimsIdentity(claims, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }
}
