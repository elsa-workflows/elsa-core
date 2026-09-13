using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Interchange.Features;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TUnit.AspNetCore;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Endpoints;

/// <summary>
/// Marker type whose assembly supplies the dependency context for the in-process BPMN API host.
/// </summary>
public sealed class BpmnInterchangeTestEntryPoint;

/// <summary>
/// Provides TUnit with a session-wide factory definition. <see cref="WebApplicationTest{TFactory,TEntryPoint}"/>
/// derives and starts a separately configured host from it for every test.
/// </summary>
public sealed class BpmnInterchangeWebApplicationFactory : TestWebApplicationFactory<BpmnInterchangeTestEntryPoint>
{
    // Avoid the default host builder's reload-on-change configuration sources. Besides making the test host depend on
    // the checkout's appsettings files, macOS FSEvents can stall while many integration hosts are starting in parallel.
    protected override IHostBuilder CreateHostBuilder() => new HostBuilder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(services =>
            {
                services.AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme, _ => { });
                services.AddAuthorization();
                services.AddRouting();
                services.AddFastEndpoints(options =>
                {
                    options.Assemblies = [typeof(BpmnInterchangeFeature).Assembly];
                    options.DisableAutoDiscovery = true;
                });
                services.AddElsa(elsa => elsa
                    .AddActivitiesFrom<WriteLine>()
                    .UseScheduling()
                    .UseCSharp(options => options.AllowHostCodeExecution = true)
                    .UseJavaScript()
                    .UseLiquid()
                    .UseWorkflowManagement()
                    .UseBpmnInterchange());
                services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
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

internal sealed class TestAuthenticationHandler(
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
            .Select(x => new Claim(PermissionNames.ClaimType, x))
            .ToList();

        claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-user"));

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
