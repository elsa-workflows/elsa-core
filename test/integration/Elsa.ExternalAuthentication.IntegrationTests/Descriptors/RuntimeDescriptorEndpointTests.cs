using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa.ExternalAuthentication.Features;
using Elsa.ExternalAuthentication.Permissions;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.IntegrationTests.Descriptors;

[Collection(nameof(RuntimeDescriptorEndpointCollection))]
public class RuntimeDescriptorEndpointTests : IAsyncLifetime
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
        builder.Services.AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(ExternalAuthenticationFeature).Assembly];
            options.Filter = endpoint => endpoint.Namespace == "Elsa.ExternalAuthentication.Endpoints.Runtime";
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
    public async Task RuntimeDescriptorRequiresReadPermissionAndReturnsSafeVersionMetadata()
    {
        using var forbidden = new HttpRequestMessage(HttpMethod.Get, "/external-authentication/descriptors/runtime");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client!.SendAsync(forbidden)).StatusCode);

        using var authorized = new HttpRequestMessage(HttpMethod.Get, "/external-authentication/descriptors/runtime");
        authorized.Headers.Add(TestAuthenticationHandler.PermissionHeader, ExternalAuthenticationPermissions.ConnectionsRead);
        var response = await _client.SendAsync(authorized);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var descriptor = await response.Content.ReadFromJsonAsync<RuntimeDescriptor>();
        Assert.NotNull(descriptor);
        Assert.Equal(1, descriptor.ManagementContractVersion);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.ProductVersion));
        Assert.False(string.IsNullOrWhiteSpace(descriptor.InformationalVersion));
    }

    private sealed record RuntimeDescriptor(int ManagementContractVersion, string ProductVersion, string InformationalVersion);

    private sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "runtime-descriptor-test";
        public const string PermissionHeader = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var permissions = Request.Headers[PermissionHeader]
                .SelectMany(x => x?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? []);
            var identity = new ClaimsIdentity(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)), AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationScheme)));
        }
    }
}

[CollectionDefinition(nameof(RuntimeDescriptorEndpointCollection), DisableParallelization = true)]
public class RuntimeDescriptorEndpointCollection;
