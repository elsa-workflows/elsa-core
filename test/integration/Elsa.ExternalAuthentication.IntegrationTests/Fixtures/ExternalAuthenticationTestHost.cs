using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa.Workflows;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.IntegrationTests.Fixtures;

public sealed class ExternalAuthenticationTestEntryPoint;

internal sealed class TestAuthenticationState
{
    private Claim[] _claims = [];

    public bool IsAuthenticated { get; set; } = true;
    public IReadOnlyCollection<Claim> Claims => _claims;

    public void SetClaims(params Claim[] claims) => _claims = claims;

    public void SetPermissions(params string[] permissions) =>
        SetClaims(permissions.Select(permission => new Claim(PermissionNames.ClaimType, permission)).ToArray());
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TestAuthenticationState state) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ExternalAuthenticationTests";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!state.IsAuthenticated)
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(state.Claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal static class ExternalAuthenticationTestHost
{
    public static void Configure(
        IWebHostBuilder builder,
        Assembly endpointAssembly,
        Func<Type, bool> endpointFilter,
        string authenticationScheme = TestAuthenticationHandler.SchemeName,
        bool addRateLimiter = false)
    {
        builder
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddLogging();
                services
                    .AddAuthentication(authenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(authenticationScheme, _ => { });
                services.AddAuthorization();
                if (addRateLimiter)
                    services.AddRateLimiter(_ => { });
                services.AddFastEndpoints(options =>
                {
                    options.Assemblies = [endpointAssembly];
                    options.Filter = endpointFilter;
                    options.DisableAutoDiscovery = true;
                });
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
