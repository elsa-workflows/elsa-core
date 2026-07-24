using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa.ExternalAuthentication.Features;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Workflows;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Links;

[Collection(nameof(IdentityLinkAuthorizationCollection))]
public class IdentityLinkAuthorizationTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;
    private bool _wasSecurityEnabled;

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = true;
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication(TestAuthenticationHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<MemoryStore<User>>();
        builder.Services.AddSingleton<IIdentityGenerator, GuidIdentityGenerator>();
        builder.Services.AddSingleton<Elsa.Common.ISystemClock, Elsa.Common.Services.SystemClock>();
        builder.Services.AddSingleton<IExternalAuthenticationHandleHasher, HmacExternalAuthenticationHandleHasher>();
        builder.Services.AddSingleton<InMemoryExternalIdentityProvisionerState>();
        builder.Services.AddSingleton<IIdentityProviderConnectionRegistry>(Substitute.For<IIdentityProviderConnectionRegistry>());
        var tenant = Substitute.For<ITenantAccessor>();
        tenant.TenantId.Returns("tenant-a");
        builder.Services.AddSingleton(tenant);
        builder.Services.AddScoped<IUserStore, MemoryUserStore>();
        builder.Services.AddScoped<IUserProvider, StoreBasedUserProvider>();
        builder.Services.AddScoped<InMemoryExternalIdentityProvisioner>();
        builder.Services.AddScoped<IExternalIdentityProvisioner>(services => services.GetRequiredService<InMemoryExternalIdentityProvisioner>());
        builder.Services.AddScoped<IExternalIdentityLinkManagementStore>(services => services.GetRequiredService<InMemoryExternalIdentityProvisioner>());
        builder.Services.AddScoped<ExternalIdentityLinkManagementService>();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(ExternalAuthenticationFeature).Assembly];
            options.Filter = endpoint => endpoint.Namespace == "Elsa.ExternalAuthentication.Endpoints.IdentityLinks";
        });
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
    public async Task UserOptionsRequiresTheLinkManagementPermissionRatherThanAnUnrelatedPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/external-authentication/user-options");
        request.Headers.Add(TestAuthenticationHandler.PermissionHeader, ExternalAuthenticationPermissions.ConnectionsRead);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client!.SendAsync(request)).StatusCode);
    }

    private sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "test";
        public const string PermissionHeader = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var permissions = Request.Headers[PermissionHeader].SelectMany(x => x?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? []);
            var identity = new ClaimsIdentity(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)), AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationScheme)));
        }
    }
}

[CollectionDefinition(nameof(IdentityLinkAuthorizationCollection), DisableParallelization = true)]
public class IdentityLinkAuthorizationCollection;
