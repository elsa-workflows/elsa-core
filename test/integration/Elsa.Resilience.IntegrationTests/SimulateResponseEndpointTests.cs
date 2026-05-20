using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa.Resilience.Endpoints.SimulateResponse;
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

public class SimulateResponseEndpointTests : IAsyncLifetime
{
    private readonly TestTimeProvider _timeProvider = new();
    private WebApplication? _app;
    private bool _wasSecurityEnabled;

    private HttpClient HttpClient { get; set; } = null!;

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = true;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddFastEndpoints(o =>
        {
            o.Assemblies = [typeof(ResilienceFeature).Assembly];
            o.DisableAutoDiscovery = true;
        });
        builder.Services.AddSingleton<TimeProvider>(_timeProvider);
        builder.Services.AddOptions<ResilienceOptions>();
        builder.Services.AddOptions<SimulateResponseOptions>().Configure(options =>
        {
            options.SessionCapacity = 2;
            options.SessionSlidingExpiration = TimeSpan.FromSeconds(1);
            options.MaxCodes = 3;
            options.MaxCodesQueryLength = 16;
            options.MaxSessionIdLength = 16;
        });
        builder.Services.AddSingleton<ResilienceStrategySerializer>();
        builder.Services.AddSingleton<SimulateResponseSessionStore>();
        builder.Services.AddScoped<IRetryAttemptReader>(_ => VoidRetryAttemptReader.Instance);
        builder.Services.AddScoped(_ =>
        {
            var catalog = Substitute.For<IResilienceStrategyCatalog>();
            catalog.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
            return catalog;
        });

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();

        await _app.StartAsync();
        HttpClient = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        HttpClient.Dispose();

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Get_WhenAnonymousAndSecurityEnabled_DoesNotCreateSessionState()
    {
        var response = await HttpClient.GetAsync("/simulate-response?sessionId=anon&codes=[500,200]");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var authenticatedResponse = await GetAuthenticatedAsync("/simulate-response?sessionId=anon&codes=[500,200]");
        Assert.Equal(HttpStatusCode.InternalServerError, authenticatedResponse.StatusCode);
    }

    [Fact]
    public async Task Get_WhenCodesAreMalformed_ReturnsBadRequest()
    {
        var response = await GetAuthenticatedAsync("/simulate-response?codes=not-json");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenSessionCapacityIsReached_ReturnsTooManyRequestsUntilStateExpires()
    {
        Assert.Equal(HttpStatusCode.InternalServerError, (await GetAuthenticatedAsync("/simulate-response?sessionId=first&codes=[500,200]")).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await GetAuthenticatedAsync("/simulate-response?sessionId=second&codes=[500,200]")).StatusCode);

        var rejected = await GetAuthenticatedAsync("/simulate-response?sessionId=third&codes=[500,200]");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

        _timeProvider.Advance(TimeSpan.FromSeconds(2));

        var acceptedAfterExpiration = await GetAuthenticatedAsync("/simulate-response?sessionId=third&codes=[500,200]");
        Assert.Equal(HttpStatusCode.InternalServerError, acceptedAfterExpiration.StatusCode);
    }

    private async Task<HttpResponseMessage> GetAuthenticatedAsync(string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add(TestAuthenticationHandler.PermissionHeader, "*");
        return await HttpClient.SendAsync(request);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.Parse("2026-05-20T00:00:00Z");

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan timeSpan)
        {
            _now = _now.Add(timeSpan);
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "Test";
        public const string PermissionHeader = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(PermissionHeader, out var permissionHeader))
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = permissionHeader
                .SelectMany(x => x?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
                .Select(x => new Claim("permissions", x))
                .ToList();

            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
