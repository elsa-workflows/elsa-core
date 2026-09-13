using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa;
using Elsa.Alterations.Endpoints.Alterations.DryRun;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TUnit.AspNetCore;
using SubmitEndpoint = Elsa.Alterations.Endpoints.Alterations.Submit.Submit;

namespace Elsa.Alterations.IntegrationTests;

public sealed class AlterationsTestEntryPoint;

public sealed class AlterationsWebApplicationFactory : TestWebApplicationFactory<AlterationsTestEntryPoint>
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
                services.AddAuthentication(AlterationsTestAuthenticationHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, AlterationsTestAuthenticationHandler>(AlterationsTestAuthenticationHandler.AuthenticationScheme, _ => { });
                services.AddAuthorization();
                services.AddRouting();
                services.AddFastEndpoints(options =>
                {
                    options.Assemblies = [typeof(DryRun).Assembly];
                    options.DisableAutoDiscovery = true;
                    options.Filter = endpointType => endpointType == typeof(DryRun) || endpointType == typeof(SubmitEndpoint);
                });
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

internal sealed class AlterationsTestAuthenticationHandler(
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
