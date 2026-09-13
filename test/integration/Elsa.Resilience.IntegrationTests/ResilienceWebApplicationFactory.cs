using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa;
using Elsa.Resilience.Endpoints.SimulateResponse;
using Elsa.Resilience.Features;
using Elsa.Resilience.Options;
using Elsa.Resilience.Serialization;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TUnit.AspNetCore;

namespace Elsa.Resilience.IntegrationTests;

public sealed class ResilienceTestEntryPoint;

public sealed class ResilienceWebApplicationFactory : TestWebApplicationFactory<ResilienceTestEntryPoint>
{
    protected override IHostBuilder CreateHostBuilder() => new HostBuilder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder
            .UseEnvironment(Environments.Development)
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(services =>
            {
                services.AddAuthentication(ResilienceTestAuthenticationHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, ResilienceTestAuthenticationHandler>(ResilienceTestAuthenticationHandler.AuthenticationScheme, _ => { });
                services.AddAuthorization();
                services.AddRouting();
                services.AddFastEndpoints(options =>
                {
                    options.Assemblies = [typeof(ResilienceFeature).Assembly];
                    options.DisableAutoDiscovery = true;
                    options.Filter = endpointType => endpointType == typeof(SimulateResponseEndpoint);
                });
                services.AddOptions<ResilienceOptions>();
                services.AddOptions<SimulateResponseOptions>().Configure(options =>
                {
                    options.SessionCapacity = 2;
                    options.SessionSlidingExpiration = TimeSpan.FromSeconds(1);
                    options.MaxCodes = 3;
                    options.MaxCodesQueryLength = 32;
                    options.MaxSessionIdLength = 16;
                });
                services.AddSingleton<ResilienceStrategySerializer>();
                services.AddSingleton<SimulateResponseSessionStore>();
                services.AddLogging();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapFastEndpoints());
            });
    }
}

internal sealed class ResilienceTestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "Test";
    public const string IdentityHeader = "X-Test-Identity";
    public const string PermissionHeader = "X-Test-Permissions";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(PermissionHeader, out var permissionHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = Request.Headers.TryGetValue(IdentityHeader, out var identityHeader)
            ? identityHeader.FirstOrDefault()
            : null;
        var claims = permissionHeader
            .SelectMany(x => x?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
            .Select(x => new Claim(PermissionNames.ClaimType, x))
            .ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, identity ?? "test-user"));

        var claimsIdentity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(claimsIdentity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
